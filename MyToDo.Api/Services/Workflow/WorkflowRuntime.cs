using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class WorkflowRuntime : IWorkflowRuntime
    {
        private readonly MyToDoContext _context;

        public WorkflowRuntime(MyToDoContext context)
        {
            _context = context;
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
            var bookmark = await _context.WorkflowBookmarks
                .FirstOrDefaultAsync(x =>
                    x.BookmarkType == bookmarkType &&
                    x.BookmarkKey == bookmarkKey &&
                    x.Status == WorkflowBookmarkStatus.Active,
                    cancellationToken);

            if (bookmark == null)
            {
                return false;
            }

            var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == bookmark.ExecutionTokenId, cancellationToken);
            var instance = await _context.WorkflowInstances.FirstAsync(x => x.Id == bookmark.WorkflowInstanceId, cancellationToken);
            var nodeInstance = await _context.WorkflowNodeInstances.FirstAsync(x => x.Id == bookmark.WorkflowNodeInstanceId, cancellationToken);

            if (bookmarkType == WorkflowBookmarkTypes.ScheduleTaskScheduled && Guid.TryParse(bookmark.BookmarkKey, out var taskId))
            {
                var task = await _context.SchedulableTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
                if (task != null && task.Status == SchedulableTaskStatus.ReadyForScheduling)
                {
                    task.Status = SchedulableTaskStatus.Scheduled;
                }
            }

            bookmark.Status = WorkflowBookmarkStatus.Consumed;
            bookmark.ConsumedAt = DateTime.UtcNow;
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
            while (true)
            {
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

                if (node.NodeType == WorkflowNodeType.End)
                {
                    nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
                    nodeInstance.CompletedAt = DateTime.UtcNow;
                    token.Status = WorkflowExecutionTokenStatus.Completed;
                    token.UpdatedAt = DateTime.UtcNow;
                    instance.Status = WorkflowInstanceStatus.Completed;
                    instance.CompletedAt = DateTime.UtcNow;

                    var workOrder = await _context.WorkOrders.FirstAsync(x => x.Id == instance.WorkOrderId, cancellationToken);
                    workOrder.Status = WorkOrderStatus.Completed;
                    workOrder.CompletedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }

                if (node.NodeType == WorkflowNodeType.Start)
                {
                    nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
                    nodeInstance.CompletedAt = DateTime.UtcNow;

                    await MoveTokenToNextNodeOrCompleteAsync(instance, token, node.Id, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    continue;
                }

                if (node.NodeType == WorkflowNodeType.ScheduleTask)
                {
                    var workOrder = await _context.WorkOrders.FirstAsync(x => x.Id == instance.WorkOrderId, cancellationToken);
                    var schedulableTask = new SchedulableTask
                    {
                        Id = Guid.NewGuid(),
                        WorkOrderId = workOrder.Id,
                        WorkflowInstanceId = instance.Id,
                        WorkflowNodeInstanceId = nodeInstance.Id,
                        RequiredResourceType = node.RequiredResourceType ?? "Workstation",
                        Priority = workOrder.Priority,
                        EarliestStartTime = workOrder.EarliestStartTime,
                        DurationMinutes = node.EstimatedDurationMinutes,
                        Status = SchedulableTaskStatus.ReadyForScheduling
                    };

                    _context.SchedulableTasks.Add(schedulableTask);

                    var bookmark = new WorkflowBookmark
                    {
                        Id = Guid.NewGuid(),
                        WorkflowInstanceId = instance.Id,
                        ExecutionTokenId = token.Id,
                        WorkflowNodeInstanceId = nodeInstance.Id,
                        BookmarkType = WorkflowBookmarkTypes.ScheduleTaskScheduled,
                        BookmarkKey = schedulableTask.Id.ToString(),
                        Status = WorkflowBookmarkStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.WorkflowBookmarks.Add(bookmark);
                    nodeInstance.Status = WorkflowNodeInstanceStatus.Waiting;
                    token.Status = WorkflowExecutionTokenStatus.Waiting;
                    token.UpdatedAt = DateTime.UtcNow;
                    instance.Status = WorkflowInstanceStatus.Suspended;
                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }

                if (node.NodeType == WorkflowNodeType.WorkstationTask)
                {
                    var bookmark = new WorkflowBookmark
                    {
                        Id = Guid.NewGuid(),
                        WorkflowInstanceId = instance.Id,
                        ExecutionTokenId = token.Id,
                        WorkflowNodeInstanceId = nodeInstance.Id,
                        BookmarkType = WorkflowBookmarkTypes.WorkstationTaskCompleted,
                        BookmarkKey = $"{instance.Id}:{node.Id}",
                        Status = WorkflowBookmarkStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.WorkflowBookmarks.Add(bookmark);
                    nodeInstance.Status = WorkflowNodeInstanceStatus.Waiting;
                    token.Status = WorkflowExecutionTokenStatus.Waiting;
                    token.UpdatedAt = DateTime.UtcNow;
                    instance.Status = WorkflowInstanceStatus.Suspended;
                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }

                throw new InvalidOperationException($"Unsupported node type: {node.NodeType}");
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
