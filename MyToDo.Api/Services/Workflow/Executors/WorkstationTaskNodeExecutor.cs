using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow.Executors
{
    /// <summary>
    /// Handles the WorkstationTask node: dispatches the task to the workstation gateway and
    /// suspends execution via a bookmark until the device reports completion.
    /// </summary>
    public class WorkstationTaskNodeExecutor : IWorkflowNodeExecutor
    {
        private readonly IWorkflowBookmarkService _bookmarkService;
        private readonly IWorkstationGateway _gateway;

        public WorkstationTaskNodeExecutor(IWorkflowBookmarkService bookmarkService, IWorkstationGateway gateway)
        {
            _bookmarkService = bookmarkService;
            _gateway = gateway;
        }

        public WorkflowNodeType NodeType => WorkflowNodeType.WorkstationTask;

        public async Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
        {
            var gatewayResult = await _gateway.StartExperimentAsync(
                context.Node.RequiredResourceType ?? "Workstation",
                cancellationToken);

            // Use the device job ID as the bookmark key so the caller can resume by job ID.
            // Fall back to instance:node format if the gateway did not return a job ID.
            var bookmarkKey = gatewayResult.Success && !string.IsNullOrEmpty(gatewayResult.DeviceJobId)
                ? gatewayResult.DeviceJobId
                : $"{context.Instance.Id}:{context.Node.Id}";

            _bookmarkService.Create(
                context.Instance.Id,
                context.Token.Id,
                context.NodeInstance.Id,
                WorkflowBookmarkTypes.WorkstationTaskCompleted,
                bookmarkKey);

            context.NodeInstance.Status = WorkflowNodeInstanceStatus.Waiting;
            context.Token.Status = WorkflowExecutionTokenStatus.Waiting;
            context.Token.UpdatedAt = DateTime.UtcNow;
            context.Instance.Status = WorkflowInstanceStatus.Suspended;

            return NodeExecutionResult.Suspend();
        }
    }
}
