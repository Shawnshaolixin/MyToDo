using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow.Executors
{
    /// <summary>
    /// Handles the ScheduleTask node: creates a <see cref="SchedulableTask"/> ready for APS scheduling
    /// and suspends execution via a bookmark until the task is scheduled.
    /// </summary>
    public class ScheduleTaskNodeExecutor : IWorkflowNodeExecutor
    {
        private readonly IWorkflowBookmarkService _bookmarkService;

        public ScheduleTaskNodeExecutor(IWorkflowBookmarkService bookmarkService)
        {
            _bookmarkService = bookmarkService;
        }

        public WorkflowNodeType NodeType => WorkflowNodeType.ScheduleTask;

        public async Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
        {
            var workOrder = await context.DbContext.WorkOrders
                .FirstAsync(x => x.Id == context.Instance.WorkOrderId, cancellationToken);

            var schedulableTask = new SchedulableTask
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrder.Id,
                WorkflowInstanceId = context.Instance.Id,
                WorkflowNodeInstanceId = context.NodeInstance.Id,
                RequiredResourceType = context.Node.RequiredResourceType ?? "Workstation",
                Priority = workOrder.Priority,
                EarliestStartTime = workOrder.EarliestStartTime,
                DurationMinutes = context.Node.EstimatedDurationMinutes,
                Status = SchedulableTaskStatus.ReadyForScheduling
            };

            context.DbContext.SchedulableTasks.Add(schedulableTask);

            _bookmarkService.Create(
                context.Instance.Id,
                context.Token.Id,
                context.NodeInstance.Id,
                WorkflowBookmarkTypes.ScheduleTaskScheduled,
                schedulableTask.Id.ToString());

            context.NodeInstance.Status = WorkflowNodeInstanceStatus.Waiting;
            context.Token.Status = WorkflowExecutionTokenStatus.Waiting;
            context.Token.UpdatedAt = DateTime.UtcNow;
            context.Instance.Status = WorkflowInstanceStatus.Suspended;

            return NodeExecutionResult.Suspend();
        }
    }
}
