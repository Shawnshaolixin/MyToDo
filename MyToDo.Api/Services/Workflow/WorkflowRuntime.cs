using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Minimal workflow runtime implementation.
    ///
    /// Flow summary:
    /// - StartAsync creates WorkflowInstance + first token at Start node.
    /// - ExecuteNodeAsync resolves executor by node type and runs it.
    /// - Done outcome moves token to next node and continues.
    /// - Waiting outcome creates bookmark and suspends instance/token.
    /// - ResumeAsync consumes bookmark, completes waiting node instance, and continues execution.
    ///
    /// Error handling:
    /// - Missing workflow nodes/executors throw InvalidOperationException.
    /// - Duplicate resume attempts return false once bookmark is already consumed.
    ///
    /// Bookmark usage:
    /// - External systems (APS or workstation callbacks) call ResumeAsync with bookmarkType + bookmarkKey.
    /// - Bookmark service finds active bookmark, marks it consumed, then runtime advances token.
    /// </summary>
    public class WorkflowRuntime : IWorkflowRuntime
    {
        private readonly MyToDoContext _context;
        private readonly IWorkflowNodeExecutorRegistry _executorRegistry;
        private readonly IWorkflowBookmarkService _bookmarkService;

        public WorkflowRuntime(
            MyToDoContext context,
            IWorkflowNodeExecutorRegistry executorRegistry,
            IWorkflowBookmarkService bookmarkService)
        {
            _context = context;
            _executorRegistry = executorRegistry;
            _bookmarkService = bookmarkService;
        }

        public async Task<WorkflowInstance> StartAsync(Guid workOrderId, Guid workflowVersionId, CancellationToken cancellationToken)
        {
            var workOrder = await _context.WorkOrders.FirstOrDefaultAsync(x => x.Id == workOrderId, cancellationToken)
                ?? throw new InvalidOperationException("WorkOrder not found.");

            var startNode = await _context.WorkflowNodes
                .FirstOrDefaultAsync(x => x.WorkflowVersionId == workflowVersionId && x.NodeType == WorkflowNodeType.Start, cancellationToken)
                ?? throw new InvalidOperationException("Start node not found.");

            workOrder.Status = WorkOrderStatus.InProgress;
            workOrder.WorkflowVersionId = workflowVersionId;

            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrderId,
                WorkflowVersionId = workflowVersionId,
                Status = WorkflowInstanceStatus.Running,
                StartedAt = DateTime.UtcNow
            };

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

            await ExecuteNodeAsync(instance.Id, token.Id, cancellationToken);
            return instance;
        }

        public async Task<bool> ResumeAsync(string bookmarkType, string bookmarkKey, object? input, CancellationToken cancellationToken)
        {
            var bookmark = await _bookmarkService.FindAsync(bookmarkType, bookmarkKey, cancellationToken);

            if (bookmark == null)
            {
                return false;
            }

            var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == bookmark.ExecutionTokenId, cancellationToken);
            var instance = await _context.WorkflowInstances.FirstAsync(x => x.Id == bookmark.WorkflowInstanceId, cancellationToken);
            var nodeInstance = await _context.WorkflowNodeInstances.FirstAsync(x => x.Id == bookmark.WorkflowNodeInstanceId, cancellationToken);
            var consumed = await _bookmarkService.ConsumeAsync(bookmark.Id, cancellationToken);
            if (!consumed)
            {
                return false;
            }

            nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
            nodeInstance.CompletedAt = DateTime.UtcNow;

            token.Status = WorkflowExecutionTokenStatus.Ready;
            token.UpdatedAt = DateTime.UtcNow;
            instance.Status = WorkflowInstanceStatus.Running;

            await MoveTokenToNextNodeOrCompleteAsync(instance, token, nodeInstance.WorkflowNodeId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            if (token.Status != WorkflowExecutionTokenStatus.Completed)
            {
                await ExecuteNodeAsync(instance.Id, token.Id, cancellationToken);
            }

            return true;
        }

        public async Task ExecuteNodeAsync(Guid workflowInstanceId, Guid executionTokenId, CancellationToken cancellationToken)
        {
            var initialInstance = await _context.WorkflowInstances
                .AsNoTracking()
                .FirstAsync(x => x.Id == workflowInstanceId, cancellationToken);
            var workOrder = await _context.WorkOrders
                .FirstAsync(x => x.Id == initialInstance.WorkOrderId, cancellationToken);

            // Loop to allow immediate pass-through over consecutive Done nodes (e.g., Start -> End).
            var iteration = 0;
            while (true)
            {
                iteration++;
                if (iteration > 1000)
                {
                    throw new InvalidOperationException("Workflow execution exceeded max iteration guard.");
                }

                var instance = await _context.WorkflowInstances.FirstAsync(x => x.Id == workflowInstanceId, cancellationToken);
                var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == executionTokenId, cancellationToken);

                if (token.Status == WorkflowExecutionTokenStatus.Completed || token.Status == WorkflowExecutionTokenStatus.Waiting)
                {
                    return;
                }

                var node = await _context.WorkflowNodes.FirstOrDefaultAsync(x => x.Id == token.CurrentNodeId, cancellationToken)
                    ?? throw new InvalidOperationException("Workflow node not found.");

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
                await _context.SaveChangesAsync(cancellationToken);

                var executionContext = new WorkflowExecutionContext
                {
                    DbContext = _context,
                    Instance = instance,
                    Token = token,
                    Node = node,
                    NodeInstance = nodeInstance,
                    WorkOrder = workOrder,
                    CancellationToken = cancellationToken
                };

                var executor = _executorRegistry.Find(node.NodeType);
                var result = await executor.ExecuteAsync(executionContext);

                if (result.Outcome == NodeExecutionOutcome.Done)
                {
                    if (nodeInstance.Status == WorkflowNodeInstanceStatus.Running)
                    {
                        nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
                        nodeInstance.CompletedAt = DateTime.UtcNow;
                    }

                    if (token.Status != WorkflowExecutionTokenStatus.Completed)
                    {
                        await MoveTokenToNextNodeOrCompleteAsync(instance, token, node.Id, cancellationToken);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    continue;
                }

                if (result.Outcome == NodeExecutionOutcome.Waiting)
                {
                    if (string.IsNullOrWhiteSpace(result.BookmarkType) || string.IsNullOrWhiteSpace(result.BookmarkKey))
                    {
                        throw new InvalidOperationException("Waiting node executor must return bookmark type and key.");
                    }

                    nodeInstance.Status = WorkflowNodeInstanceStatus.Waiting;
                    token.Status = WorkflowExecutionTokenStatus.Waiting;
                    token.UpdatedAt = DateTime.UtcNow;
                    instance.Status = WorkflowInstanceStatus.Suspended;

                    var bookmark = new WorkflowBookmark
                    {
                        Id = Guid.NewGuid(),
                        WorkflowInstanceId = instance.Id,
                        ExecutionTokenId = token.Id,
                        WorkflowNodeInstanceId = nodeInstance.Id,
                        BookmarkType = result.BookmarkType,
                        BookmarkKey = result.BookmarkKey,
                        InputJson = result.InputJson,
                        Status = WorkflowBookmarkStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _bookmarkService.CreateAsync(bookmark, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }

                nodeInstance.Status = WorkflowNodeInstanceStatus.Failed;
                token.Status = WorkflowExecutionTokenStatus.Failed;
                instance.Status = WorkflowInstanceStatus.Failed;
                workOrder.Status = WorkOrderStatus.Failed;
                await _context.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException($"Node execution failed for type: {node.NodeType}");
            }
        }

        private async Task MoveTokenToNextNodeOrCompleteAsync(
            WorkflowInstance instance,
            WorkflowExecutionToken token,
            Guid currentNodeId,
            CancellationToken cancellationToken)
        {
            var nextEdge = await _context.WorkflowEdges
                .Where(x => x.WorkflowVersionId == instance.WorkflowVersionId && x.FromNodeId == currentNodeId)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextEdge == null)
            {
                token.Status = WorkflowExecutionTokenStatus.Completed;
                token.UpdatedAt = DateTime.UtcNow;
                instance.Status = WorkflowInstanceStatus.Completed;
                instance.CompletedAt = DateTime.UtcNow;

                var workOrder = await _context.WorkOrders.FirstAsync(x => x.Id == instance.WorkOrderId, cancellationToken);
                workOrder.Status = WorkOrderStatus.Completed;
                workOrder.CompletedAt = DateTime.UtcNow;
                return;
            }

            token.CurrentNodeId = nextEdge.ToNodeId;
            token.Status = WorkflowExecutionTokenStatus.Ready;
            token.UpdatedAt = DateTime.UtcNow;
            instance.Status = WorkflowInstanceStatus.Running;
        }
    }
}
