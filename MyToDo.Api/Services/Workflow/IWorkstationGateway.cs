namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Abstraction over the physical workstation controller.
    ///
    /// Implementations are responsible for dispatching experiment jobs to
    /// physical devices (via HTTP, MQTT, etc.) and returning a device-assigned
    /// job identifier that can be used to correlate completion events.
    ///
    /// Usage in workflow:
    ///   1. <see cref="WorkstationTaskExecutor"/> calls <see cref="StartExperimentAsync"/>
    ///      when the workflow reaches a WorkstationTask node.
    ///   2. The device runs the experiment and later sends a completion event.
    ///   3. The event handler calls <see cref="IWorkflowRuntime.ResumeAsync"/> with
    ///      the bookmark type <see cref="WorkflowBookmarkTypes.WorkstationTaskCompleted"/>.
    /// </summary>
    public interface IWorkstationGateway
    {
        /// <summary>
        /// Starts an experiment on the workstation associated with
        /// <paramref name="workflowNodeId"/> within workflow instance
        /// <paramref name="workflowInstanceId"/>.
        /// </summary>
        /// <param name="workflowInstanceId">The running workflow instance.</param>
        /// <param name="workflowNodeId">The node definition being executed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// A <c>DeviceJobId</c> (GUID) assigned by the device controller.
        /// Store this alongside the bookmark so incoming device events can be
        /// mapped back to the correct bookmark key.
        /// </returns>
        Task<Guid> StartExperimentAsync(
            Guid workflowInstanceId,
            Guid workflowNodeId,
            CancellationToken cancellationToken);
    }
}
