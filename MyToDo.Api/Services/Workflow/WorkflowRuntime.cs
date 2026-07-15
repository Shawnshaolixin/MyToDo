using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Core workflow execution engine.
    ///
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Create and initialise a <see cref="WorkflowInstance"/> when a work order
    ///         is started (<see cref="StartAsync"/>).</item>
    ///   <item>Drive the execution loop: resolve the current node's executor from
    ///         <see cref="IWorkflowNodeExecutorRegistry"/>, delegate execution, then
    ///         advance the token along workflow edges until the flow suspends or ends.</item>
    ///   <item>Resume suspended flows when an external event fires
    ///         (<see cref="ResumeAsync"/>).</item>
    /// </list>
    ///
    /// Bookmark flow:
    ///   When a node executor returns <see cref="NodeExecutionOutcome.Waiting"/>, it
    ///   has already created and persisted a <see cref="WorkflowBookmark"/> via
    ///   <see cref="IWorkflowBookmarkService.CreateAsync"/>.  The runtime stops looping.
    ///   Later, an external caller (APS scheduler result, device event, etc.) calls
    ///   <see cref="ResumeAsync"/> which consumes the bookmark and continues execution.
    ///
    /// Error cases:
    ///   - Missing work order or start node → <see cref="InvalidOperationException"/>.
    ///   - No executor registered for a node type → <see cref="InvalidOperationException"/>.
    ///   - More than 1000 execution iterations → guard throws to prevent infinite loops
    ///     caused by a circular workflow definition.
    /// </summary>
    public class WorkflowRuntime : IWorkflowRuntime
    {
        private readonly MyToDoContext _context;
        private readonly IWorkflowNodeExecutorRegistry _registry;
        private readonly IWorkflowBookmarkService _bookmarkService;

        public WorkflowRuntime(
            MyToDoContext context,
            IWorkflowNodeExecutorRegistry registry,
            IWorkflowBookmarkService bookmarkService)
        {
            _context = context;
            _registry = registry;
            _bookmarkService = bookmarkService;
        }

        /// <summary>
        /// Creates a new <see cref="WorkflowInstance"/> for the given work order,
        /// places an execution token on the Start node, and immediately begins
        /// executing nodes until the flow suspends or finishes.
        /// </summary>
        public async Task<WorkflowInstance> StartAsync(
            Guid workOrderId,
            Guid workflowVersionId,
            CancellationToken cancellationToken)
        {
            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == workOrderId, cancellationToken)
                ?? throw new InvalidOperationException("WorkOrder not found.");

            var startNode = await _context.WorkflowNodes
                .FirstOrDefaultAsync(
                    x => x.WorkflowVersionId == workflowVersionId && x.NodeType == WorkflowNodeType.Start,
                    cancellationToken)
                ?? throw new InvalidOperationException("Start node not found.");

            // Mark the work order as in-progress and bind it to the requested version
            workOrder.Status = WorkOrderStatus.InProgress;
            workOrder.WorkflowVersionId = workflowVersionId;

            // Create the runtime instance record
            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrderId,
                WorkflowVersionId = workflowVersionId,
                Status = WorkflowInstanceStatus.Running,
                StartedAt = DateTime.UtcNow
            };

            // Create the single execution token starting at the Start node
            var token = new WorkflowExecutionToken
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instance.Id,
                CurrentNodeId = startNode.Id,
                Status = WorkflowExecutionTokenStatus.Ready,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.WorkflowInstances.Add(instance);
            _context.WorkflowExecutionTokens.Add(token);
            await _context.SaveChangesAsync(cancellationToken);

            // Begin execution from the Start node
            await ExecuteNodeAsync(instance.Id, token.Id, cancellationToken);
            return instance;
        }

        /// <summary>
        /// Resumes a suspended workflow by consuming the active bookmark matching
        /// <paramref name="bookmarkType"/> + <paramref name="bookmarkKey"/>, advancing
        /// the token to the next node, and continuing execution.
        /// </summary>
        /// <returns><c>true</c> if a matching active bookmark was found and consumed.</returns>
        public async Task<bool> ResumeAsync(
            string bookmarkType,
            string bookmarkKey,
            object? input,
            CancellationToken cancellationToken)
        {
            // Look up the active bookmark via the service (uses the composite index)
            var bookmark = await _bookmarkService.FindAsync(bookmarkType, bookmarkKey, cancellationToken);
            if (bookmark == null)
            {
                return false;
            }

            // For ScheduleTask bookmarks, validate that the task has actually been scheduled
            // before allowing the workflow to advance — prevents premature resumption.
            if (bookmarkType == WorkflowBookmarkTypes.ScheduleTaskScheduled &&
                Guid.TryParse(bookmark.BookmarkKey, out var taskId))
            {
                var task = await _context.SchedulableTasks
                    .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
                if (task == null || task.Status != SchedulableTaskStatus.Scheduled)
                {
                    return false;
                }
            }

            var token = await _context.WorkflowExecutionTokens
                .FirstAsync(x => x.Id == bookmark.ExecutionTokenId, cancellationToken);
            var instance = await _context.WorkflowInstances
                .FirstAsync(x => x.Id == bookmark.WorkflowInstanceId, cancellationToken);
            var nodeInstance = await _context.WorkflowNodeInstances
                .FirstAsync(x => x.Id == bookmark.WorkflowNodeInstanceId, cancellationToken);

            // Consume the bookmark (marks it so it cannot be resumed a second time)
            _bookmarkService.Consume(bookmark);

            // Mark the suspended node instance as completed
            nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
            nodeInstance.CompletedAt = DateTime.UtcNow;

            // Reactivate the token and the instance so execution can continue
            token.Status = WorkflowExecutionTokenStatus.Ready;
            token.UpdatedAt = DateTime.UtcNow;
            instance.Status = WorkflowInstanceStatus.Running;

            // Advance the token along the first outgoing edge from the resumed node
            await MoveTokenToNextNodeOrCompleteAsync(instance, token, nodeInstance.WorkflowNodeId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // If the token is still active (not completed by MoveToken), keep executing
            if (token.Status != WorkflowExecutionTokenStatus.Completed)
            {
                await ExecuteNodeAsync(instance.Id, token.Id, cancellationToken);
            }

            return true;
        }

        /// <summary>
        /// Main execution loop.  For each iteration:
        /// <list type="number">
        ///   <item>Load the current instance + token state from the DB.</item>
        ///   <item>Stop if the token is Completed or Waiting.</item>
        ///   <item>Create a <see cref="WorkflowNodeInstance"/> record for this execution.</item>
        ///   <item>Resolve the executor via the registry and call it.</item>
        ///   <item>On Waiting: the executor already saved; return.</item>
        ///   <item>On Done + token Completed (End node): update WorkOrder and return.</item>
        ///   <item>On Done + token Ready: move to next node and loop.</item>
        /// </list>
        /// The 1000-iteration guard prevents infinite loops caused by circular graphs.
        /// </summary>
        public async Task ExecuteNodeAsync(
            Guid workflowInstanceId,
            Guid executionTokenId,
            CancellationToken cancellationToken)
        {
            var iteration = 0;
            while (true)
            {
                iteration++;
                if (iteration > 1000)
                {
                    throw new InvalidOperationException(
                        "Workflow execution exceeded max iteration guard (1000). " +
                        "Check for circular edges in the workflow definition.");
                }

                var instance = await _context.WorkflowInstances
                    .FirstAsync(x => x.Id == workflowInstanceId, cancellationToken);
                var token = await _context.WorkflowExecutionTokens
                    .FirstAsync(x => x.Id == executionTokenId, cancellationToken);

                // Stop if the token reached a terminal or suspended state
                if (token.Status is WorkflowExecutionTokenStatus.Completed
                                  or WorkflowExecutionTokenStatus.Waiting)
                {
                    return;
                }

                var node = await _context.WorkflowNodes
                    .FirstOrDefaultAsync(x => x.Id == token.CurrentNodeId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"WorkflowNode {token.CurrentNodeId} not found for token {executionTokenId}.");

                // Create a runtime record for this node execution
                var nodeInstance = new WorkflowNodeInstance
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = instance.Id,
                    WorkflowNodeId = node.Id,
                    ExecutionTokenId = token.Id,
                    Status = WorkflowNodeInstanceStatus.Running,
                    StartedAt = DateTime.UtcNow
                };
                _context.WorkflowNodeInstances.Add(nodeInstance);

                // Delegate all node-type-specific logic to the registered executor
                var executor = _registry.Resolve(node.NodeType)
                    ?? throw new InvalidOperationException(
                        $"No executor registered for WorkflowNodeType.{node.NodeType}. " +
                        $"Register one in Program.cs via registry.Register(...).");

                var ctx = new NodeExecutionContext
                {
                    Instance = instance,
                    Token = token,
                    Node = node,
                    NodeInstance = nodeInstance
                };

                var result = await executor.ExecuteAsync(ctx, cancellationToken);

                if (result.Outcome == NodeExecutionOutcome.Waiting)
                {
                    // The executor has already persisted the bookmark and status changes
                    // via IWorkflowBookmarkService.CreateAsync — nothing more to save here.
                    return;
                }

                // Outcome is Done — check if the token is now complete (End node)
                if (token.Status == WorkflowExecutionTokenStatus.Completed)
                {
                    // Check instance.Status on the in-memory tracked entity rather than
                    // querying the DB: at this point EndNodeExecutor has set the status
                    // but SaveChangesAsync has not yet been called, so a DB query would
                    // return stale data (previous status).
                    if (instance.Status == WorkflowInstanceStatus.Completed)
                    {
                        var workOrder = await _context.WorkOrders
                            .FirstAsync(x => x.Id == instance.WorkOrderId, cancellationToken);
                        workOrder.Status = WorkOrderStatus.Completed;
                        workOrder.CompletedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }

                // Done, token still active → move to the next node and continue looping.
                // This handles pass-through nodes like Start without extra round-trips.
                await MoveTokenToNextNodeOrCompleteAsync(instance, token, node.Id, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Moves the execution token from <paramref name="currentNodeId"/> to the first
        /// outgoing edge's target node.  If no outgoing edge exists (dangling node in the
        /// graph), the token and instance are marked Completed and the work order is finalised.
        ///
        /// Simple edge selection: the first edge ordered by <see cref="WorkflowEdge.Id"/>
        /// is chosen.  A production engine would evaluate edge conditions (e.g. IsDefault,
        /// expression results) to support branching/XOR gateways.
        /// </summary>
        private async Task MoveTokenToNextNodeOrCompleteAsync(
            WorkflowInstance instance,
            WorkflowExecutionToken token,
            Guid currentNodeId,
            CancellationToken cancellationToken)
        {
            // Select the first outgoing edge (add condition evaluation here for branching)
            var nextEdge = await _context.WorkflowEdges
                .Where(x => x.WorkflowVersionId == instance.WorkflowVersionId
                            && x.FromNodeId == currentNodeId)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextEdge == null)
            {
                // No further edges — treat as implicit completion (defensive fallback)
                token.Status = WorkflowExecutionTokenStatus.Completed;
                token.UpdatedAt = DateTime.UtcNow;
                instance.Status = WorkflowInstanceStatus.Completed;
                instance.CompletedAt = DateTime.UtcNow;

                var workOrder = await _context.WorkOrders
                    .FirstAsync(x => x.Id == instance.WorkOrderId, cancellationToken);
                workOrder.Status = WorkOrderStatus.Completed;
                workOrder.CompletedAt = DateTime.UtcNow;
                return;
            }

            // Advance token to the target node and keep it Ready for the next iteration
            token.CurrentNodeId = nextEdge.ToNodeId;
            token.Status = WorkflowExecutionTokenStatus.Ready;
            token.UpdatedAt = DateTime.UtcNow;
            instance.Status = WorkflowInstanceStatus.Running;
        }
    }
}
