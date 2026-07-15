using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class WorkflowRuntime : IWorkflowRuntime
    {
        private readonly MyToDoContext _context;
        private readonly WorkflowNodeExecutorRegistry _registry;
        private readonly IWorkflowBookmarkService _bookmarkService;

        public WorkflowRuntime(
            MyToDoContext context,
            WorkflowNodeExecutorRegistry registry,
            IWorkflowBookmarkService bookmarkService)
        {
            _context = context;
            _registry = registry;
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
                return false;

            var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == bookmark.ExecutionTokenId, cancellationToken);
            var instance = await _context.WorkflowInstances.FirstAsync(x => x.Id == bookmark.WorkflowInstanceId, cancellationToken);
            var nodeInstance = await _context.WorkflowNodeInstances.FirstAsync(x => x.Id == bookmark.WorkflowNodeInstanceId, cancellationToken);

            if (bookmarkType == WorkflowBookmarkTypes.ScheduleTaskScheduled && Guid.TryParse(bookmark.BookmarkKey, out var taskId))
            {
                var task = await _context.SchedulableTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
                if (task == null || task.Status != SchedulableTaskStatus.Scheduled)
                    return false;
            }

            _bookmarkService.Consume(bookmark);
            nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
            nodeInstance.CompletedAt = DateTime.UtcNow;

            token.Status = WorkflowExecutionTokenStatus.Ready;
            token.UpdatedAt = DateTime.UtcNow;
            instance.Status = WorkflowInstanceStatus.Running;

            await MoveTokenToNextNodeOrCompleteAsync(instance, token, nodeInstance.WorkflowNodeId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            if (token.Status != WorkflowExecutionTokenStatus.Completed)
                await ExecuteNodeAsync(instance.Id, token.Id, cancellationToken);

            return true;
        }

        public async Task ExecuteNodeAsync(Guid workflowInstanceId, Guid executionTokenId, CancellationToken cancellationToken)
        {
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

                var executor = _registry.GetExecutor(node.NodeType);
                var execCtx = new NodeExecutionContext
                {
                    Instance = instance,
                    Token = token,
                    Node = node,
                    NodeInstance = nodeInstance,
                    DbContext = _context
                };

                var result = await executor.ExecuteAsync(execCtx, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                if (result.Outcome == NodeExecutionOutcome.Suspended || result.Outcome == NodeExecutionOutcome.Terminated)
                {
                    return;
                }

                // Outcome.Completed: advance token to the next node and loop.
                await MoveTokenToNextNodeOrCompleteAsync(instance, token, node.Id, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
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
