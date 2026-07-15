using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Executor for <see cref="WorkflowNodeType.WorkstationTask"/> nodes.
    ///
    /// When the workflow reaches a WorkstationTask node this executor:
    /// <list type="number">
    ///   <item>Calls <see cref="IWorkstationGateway.StartExperimentAsync"/> to dispatch
    ///         the task to the physical workstation (or a fake gateway during development)
    ///         and receives a <c>DeviceJobId</c> for tracking.</item>
    ///   <item>Creates a <see cref="WorkflowBookmark"/> of type
    ///         <see cref="WorkflowBookmarkTypes.WorkstationTaskCompleted"/> so the workflow
    ///         can be resumed when the device signals completion.</item>
    ///   <item>Persists all staged changes atomically via
    ///         <see cref="IWorkflowBookmarkService.CreateAsync"/> and returns
    ///         <see cref="NodeExecutionOutcome.Waiting"/>.</item>
    /// </list>
    /// To resume the workflow, call
    /// <see cref="IWorkflowRuntime.ResumeAsync(string, string, object?, CancellationToken)"/>
    /// with <see cref="WorkflowBookmarkTypes.WorkstationTaskCompleted"/> and the
    /// bookmark key <c>"{instanceId}:{nodeId}"</c>.
    /// </summary>
    public class WorkstationTaskExecutor : IWorkflowNodeExecutor
    {
        private readonly IWorkstationGateway _gateway;
        private readonly IWorkflowBookmarkService _bookmarkService;

        public WorkstationTaskExecutor(IWorkstationGateway gateway, IWorkflowBookmarkService bookmarkService)
        {
            _gateway = gateway;
            _bookmarkService = bookmarkService;
        }

        /// <inheritdoc/>
        public WorkflowNodeType NodeType => WorkflowNodeType.WorkstationTask;

        /// <inheritdoc/>
        public async Task<NodeExecutionResult> ExecuteAsync(
            NodeExecutionContext context,
            CancellationToken cancellationToken)
        {
            // Dispatch the experiment to the workstation and capture the device job identifier.
            // In production the gateway sends an HTTP/MQTT request to the device controller;
            // FakeWorkstationGateway returns a deterministic GUID for local testing.
            var deviceJobId = await _gateway.StartExperimentAsync(
                context.Instance.Id,
                context.Node.Id,
                cancellationToken);

            // The bookmark key combines instanceId + nodeId so the resume endpoint can
            // target the correct suspended node even if the same workflow runs multiple times.
            // A production system might also persist deviceJobId to correlate device events.
            var bookmark = new WorkflowBookmark
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = context.Instance.Id,
                ExecutionTokenId = context.Token.Id,
                WorkflowNodeInstanceId = context.NodeInstance.Id,
                BookmarkType = WorkflowBookmarkTypes.WorkstationTaskCompleted,
                BookmarkKey = $"{context.Instance.Id}:{context.Node.Id}",
                Status = WorkflowBookmarkStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            // Update status before persisting so the atomic write captures everything
            context.NodeInstance.Status = WorkflowNodeInstanceStatus.Waiting;
            context.Token.Status = WorkflowExecutionTokenStatus.Waiting;
            context.Token.UpdatedAt = DateTime.UtcNow;
            context.Instance.Status = WorkflowInstanceStatus.Suspended;

            // CreateAsync flushes bookmark + status updates atomically
            await _bookmarkService.CreateAsync(bookmark, cancellationToken);

            // DeviceJobId is captured above but not stored in this minimal implementation.
            // A production system should persist it (e.g. on WorkflowNodeInstance.MetaJson)
            // so incoming device events can be mapped back to the bookmark key.
            _ = deviceJobId;

            return NodeExecutionResult.Waiting();
        }
    }
}
