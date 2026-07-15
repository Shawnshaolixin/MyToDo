using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class WorkstationTaskExecutor : IWorkflowNodeExecutor
    {
        private readonly IWorkstationGateway _workstationGateway;

        public WorkstationTaskExecutor(IWorkstationGateway workstationGateway)
        {
            _workstationGateway = workstationGateway;
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
                return NodeExecutionResult.Failed("Failed to start workstation experiment.");
            }

            return NodeExecutionResult.Waiting(
                WorkflowBookmarkTypes.WorkstationTaskCompleted,
                startExperimentResponse.DeviceJobId);
        }
    }
}
