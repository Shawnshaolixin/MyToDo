using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
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

            await AdvanceUntilPauseOrCompleteAsync(instance.Id, token.Id, cancellationToken);
            return instance;
        }

        public async Task<bool> ResumeAsync(string bookmarkType, string bookmarkKey, object? input, CancellationToken cancellationToken)
        {
            var bookmark = await _bookmarkService.FindAsync(bookmarkType, bookmarkKey, cancellationToken);
            if (bookmark == null)
            {
                return false;
            }

            if (bookmarkType == WorkflowBookmarkTypes.ScheduleTaskScheduled && Guid.TryParse(bookmark.BookmarkKey, out var taskId))
            {
                var task = await _context.SchedulableTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
                if (task == null || task.Status != SchedulableTaskStatus.Scheduled)
                {
                    return false;
                }
            }

            var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == bookmark.ExecutionTokenId, cancellationToken);
            var instance = await _context.WorkflowInstances.FirstAsync(x => x.Id == bookmark.WorkflowInstanceId, cancellationToken);
            var nodeInstance = await _context.WorkflowNodeInstances.FirstAsync(x => x.Id == bookmark.WorkflowNodeInstanceId, cancellationToken);

            await _bookmarkService.ConsumeAsync(bookmark, input, cancellationToken);

            nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
            nodeInstance.CompletedAt = DateTime.UtcNow;
            token.Status = WorkflowExecutionTokenStatus.Ready;
            token.UpdatedAt = DateTime.UtcNow;
            instance.Status = WorkflowInstanceStatus.Running;

            await MoveTokenToNextNodeOrCompleteAsync(instance, token, nodeInstance.WorkflowNodeId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await AdvanceUntilPauseOrCompleteAsync(instance.Id, token.Id, cancellationToken);
            return true;
        }

        public async Task ExecuteNodeAsync(Guid workflowInstanceId, Guid executionTokenId, CancellationToken cancellationToken)
        {
            var instance = await _context.WorkflowInstances.FirstAsync(x => x.Id == workflowInstanceId, cancellationToken);
            var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == executionTokenId, cancellationToken);
            var workOrder = await _context.WorkOrders.FirstAsync(x => x.Id == instance.WorkOrderId, cancellationToken);

            if (token.Status != WorkflowExecutionTokenStatus.Ready)
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

            var executor = _executorRegistry.GetExecutor(node.NodeType);
            var executionResult = await executor.ExecuteAsync(
                new WorkflowNodeExecutionContext
                {
                    WorkflowInstance = instance,
                    ExecutionToken = token,
                    WorkflowNode = node,
                    WorkflowNodeInstance = nodeInstance,
                    WorkOrder = workOrder
                },
                cancellationToken);

            if (executionResult.Status == NodeExecutionStatus.Done)
            {
                nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
                nodeInstance.CompletedAt = DateTime.UtcNow;

                if (token.Status != WorkflowExecutionTokenStatus.Completed)
                {
                    await MoveTokenToNextNodeOrCompleteAsync(instance, token, node.Id, cancellationToken);
                }
                else
                {
                    await CompleteWorkflowIfNoActiveTokensAsync(instance, token.Id, cancellationToken);
                }
            }
            else if (executionResult.Status == NodeExecutionStatus.Waiting)
            {
                if (string.IsNullOrWhiteSpace(executionResult.BookmarkType) || string.IsNullOrWhiteSpace(executionResult.BookmarkKey))
                {
                    throw new InvalidOperationException("Waiting result must include bookmark type and key.");
                }

                nodeInstance.Status = WorkflowNodeInstanceStatus.Waiting;
                token.Status = WorkflowExecutionTokenStatus.Waiting;
                token.UpdatedAt = DateTime.UtcNow;
                instance.Status = WorkflowInstanceStatus.Suspended;

                await _context.SaveChangesAsync(cancellationToken);
                await _bookmarkService.CreateAsync(
                    instance.Id,
                    token.Id,
                    nodeInstance.Id,
                    executionResult.BookmarkType,
                    executionResult.BookmarkKey,
                    executionResult.BookmarkInput,
                    cancellationToken);
                return;
            }
            else
            {
                nodeInstance.Status = WorkflowNodeInstanceStatus.Failed;
                nodeInstance.CompletedAt = DateTime.UtcNow;
                token.Status = WorkflowExecutionTokenStatus.Failed;
                token.UpdatedAt = DateTime.UtcNow;
                instance.Status = WorkflowInstanceStatus.Failed;
                workOrder.Status = WorkOrderStatus.Failed;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task AdvanceUntilPauseOrCompleteAsync(Guid workflowInstanceId, Guid executionTokenId, CancellationToken cancellationToken)
        {
            while (true)
            {
                var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == executionTokenId, cancellationToken);
                if (token.Status != WorkflowExecutionTokenStatus.Ready)
                {
                    return;
                }

                await ExecuteNodeAsync(workflowInstanceId, executionTokenId, cancellationToken);
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
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextEdge == null)
            {
                token.Status = WorkflowExecutionTokenStatus.Completed;
                token.UpdatedAt = DateTime.UtcNow;

                await CompleteWorkflowIfNoActiveTokensAsync(instance, token.Id, cancellationToken);
                return;
            }

            token.CurrentNodeId = nextEdge.ToNodeId;
            token.Status = WorkflowExecutionTokenStatus.Ready;
            token.UpdatedAt = DateTime.UtcNow;
            instance.Status = WorkflowInstanceStatus.Running;
        }

        private async Task CompleteWorkflowIfNoActiveTokensAsync(
            WorkflowInstance instance,
            Guid completedTokenId,
            CancellationToken cancellationToken)
        {
            var hasActiveTokens = await _context.WorkflowExecutionTokens
                .AnyAsync(x =>
                    x.WorkflowInstanceId == instance.Id &&
                    x.Id != completedTokenId &&
                    (x.Status == WorkflowExecutionTokenStatus.Ready || x.Status == WorkflowExecutionTokenStatus.Waiting),
                    cancellationToken);

            if (hasActiveTokens)
            {
                return;
            }

            instance.Status = WorkflowInstanceStatus.Completed;
            instance.CompletedAt = DateTime.UtcNow;

            var workOrder = await _context.WorkOrders.FirstAsync(x => x.Id == instance.WorkOrderId, cancellationToken);
            workOrder.Status = WorkOrderStatus.Completed;
            workOrder.CompletedAt = DateTime.UtcNow;
        }
    }
}
