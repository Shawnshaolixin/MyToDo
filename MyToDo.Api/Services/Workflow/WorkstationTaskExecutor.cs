using MyToDo.Api.Entities.Workflow;
using Microsoft.Extensions.Logging;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// 调用工位系统启动实验，并等待工位回传完成事件后再继续流程。
    /// </summary>
    public class WorkstationTaskExecutor : IWorkflowNodeExecutor
    {
        private readonly IWorkstationGateway _workstationGateway;
        private readonly ILogger<WorkstationTaskExecutor> _logger;

        public WorkstationTaskExecutor(
            IWorkstationGateway workstationGateway,
            ILogger<WorkstationTaskExecutor> logger)
        {
            _workstationGateway = workstationGateway;
            _logger = logger;
        }

        public WorkflowNodeType NodeType => WorkflowNodeType.WorkstationTask;

        public async Task<NodeExecutionResult> ExecuteAsync(WorkflowNodeExecutionContext context, CancellationToken cancellationToken)
        {
            var startExperimentResponse = await _workstationGateway.StartExperimentAsync(
                new StartExperimentRequest(
                    context.WorkflowInstance.Id,
                    context.WorkOrder.Id,
                    context.WorkflowNode.NodeKey),
                cancellationToken);

            if (!startExperimentResponse.Success || string.IsNullOrWhiteSpace(startExperimentResponse.DeviceJobId))
            {
                // 返回 Failed，表示外部工位任务没有成功创建，流程不能安全地继续推进。
                _logger.LogError(
                    "Failed to start workstation experiment. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, WorkOrderId={WorkOrderId}, NodeType={NodeType}",
                    context.WorkflowInstance.Id,
                    context.ExecutionToken.Id,
                    context.WorkflowNode.Id,
                    context.WorkOrder.Id,
                    context.WorkflowNode.NodeType);

                return NodeExecutionResult.Failed("Failed to start workstation experiment.");
            }

            // 返回 Waiting，表示流程已交给外部工位系统处理，需等待回调事件恢复。
            _logger.LogInformation(
                "Started workstation experiment and waiting for completion. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, WorkOrderId={WorkOrderId}, BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}",
                context.WorkflowInstance.Id,
                context.ExecutionToken.Id,
                context.WorkflowNode.Id,
                context.WorkOrder.Id,
                WorkflowBookmarkTypes.WorkstationTaskCompleted,
                startExperimentResponse.DeviceJobId);

            return NodeExecutionResult.Waiting(
                WorkflowBookmarkTypes.WorkstationTaskCompleted,
                startExperimentResponse.DeviceJobId);
        }
    }
}
