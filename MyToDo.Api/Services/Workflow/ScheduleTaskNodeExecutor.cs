using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// ScheduleTask creates a schedulable task and suspends workflow until APS scheduler allocates a resource.
    /// </summary>
    public class ScheduleTaskNodeExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.ScheduleTask;

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext context)
        {
            var schedulableTask = new SchedulableTask
            {
                Id = Guid.NewGuid(),
                WorkOrderId = context.WorkOrder.Id,
                WorkflowInstanceId = context.Instance.Id,
                WorkflowNodeInstanceId = context.NodeInstance.Id,
                RequiredResourceType = context.Node.RequiredResourceType ?? "Workstation",
                Priority = context.WorkOrder.Priority,
                EarliestStartTime = context.WorkOrder.EarliestStartTime,
                DurationMinutes = context.Node.EstimatedDurationMinutes,
                Status = SchedulableTaskStatus.ReadyForScheduling
            };

            context.DbContext.SchedulableTasks.Add(schedulableTask);
            return Task.FromResult(NodeExecutionResult.Waiting(WorkflowBookmarkTypes.ScheduleTaskScheduled, schedulableTask.Id.ToString()));
        }
    }
}
