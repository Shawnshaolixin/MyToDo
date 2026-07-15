using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class ScheduleTaskNodeExecutor : IWorkflowNodeExecutor
    {
        private readonly MyToDoContext _context;

        public ScheduleTaskNodeExecutor(MyToDoContext context)
        {
            _context = context;
        }

        public WorkflowNodeType NodeType => WorkflowNodeType.ScheduleTask;

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowNodeExecutionContext context, CancellationToken cancellationToken)
        {
            var schedulableTask = new SchedulableTask
            {
                Id = Guid.NewGuid(),
                WorkOrderId = context.WorkOrder.Id,
                WorkflowInstanceId = context.WorkflowInstance.Id,
                WorkflowNodeInstanceId = context.WorkflowNodeInstance.Id,
                RequiredResourceType = context.WorkflowNode.RequiredResourceType ?? "Workstation",
                Priority = context.WorkOrder.Priority,
                EarliestStartTime = context.WorkOrder.EarliestStartTime,
                DurationMinutes = context.WorkflowNode.EstimatedDurationMinutes,
                Status = SchedulableTaskStatus.ReadyForScheduling
            };

            _context.SchedulableTasks.Add(schedulableTask);

            return Task.FromResult(
                NodeExecutionResult.Waiting(
                    WorkflowBookmarkTypes.ScheduleTaskScheduled,
                    schedulableTask.Id.ToString("D")));
        }
    }
}
