using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;
using Microsoft.Extensions.Logging;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// 把流程节点转换成可排程任务，并进入等待 APS 排程结果的状态。
    /// </summary>
    public class ScheduleTaskNodeExecutor : IWorkflowNodeExecutor
    {
        private readonly MyToDoContext _context;
        private readonly ILogger<ScheduleTaskNodeExecutor> _logger;

        public ScheduleTaskNodeExecutor(MyToDoContext context, ILogger<ScheduleTaskNodeExecutor> logger)
        {
            _context = context;
            _logger = logger;
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

            // 进入 Waiting，表示流程已把任务提交给排程系统，后续必须依赖排程结果恢复。
            _logger.LogInformation(
                "Created schedulable task and waiting for APS scheduling. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, WorkOrderId={WorkOrderId}, BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}, RequiredResourceType={RequiredResourceType}",
                context.WorkflowInstance.Id,
                context.ExecutionToken.Id,
                context.WorkflowNode.Id,
                context.WorkOrder.Id,
                WorkflowBookmarkTypes.ScheduleTaskScheduled,
                schedulableTask.Id,
                schedulableTask.RequiredResourceType);

            return Task.FromResult(
                NodeExecutionResult.Waiting(
                    WorkflowBookmarkTypes.ScheduleTaskScheduled,
                    schedulableTask.Id.ToString("D")));
        }
    }
}
