namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Fake implementation of <see cref="IWorkstationGateway"/> for local development
    /// and integration testing.
    ///
    /// Instead of sending a real request to a physical device controller, this
    /// implementation returns deterministic fake data so the full
    /// Start → ScheduleTask → WorkstationTask → End workflow can be exercised
    /// without any hardware dependencies.
    ///
    /// To find and swap in this implementation at runtime, locate it via DI:
    ///   <c>services.AddScoped&lt;IWorkstationGateway, FakeWorkstationGateway&gt;()</c>
    /// registered in <c>Program.cs</c>.
    /// For a real deployment, replace the registration with a production gateway
    /// (e.g. <c>HttpWorkstationGateway</c>) that posts to the device controller API.
    ///
    /// How to trigger device completion events in local testing:
    ///   POST /api/WorkflowEngine/resume
    ///   { "bookmarkType": "WorkstationTaskCompleted",
    ///     "bookmarkKey": "{instanceId}:{nodeId}" }
    /// Use <c>GET /api/WorkflowEngine/instances/{instanceId}</c> to look up the
    /// active bookmark key.
    /// </summary>
    public class FakeWorkstationGateway : IWorkstationGateway
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Returns a new <see cref="Guid"/> as the device job identifier.
        /// In production this would be the job ID returned by the device controller's
        /// REST API or MQTT acknowledgement message.
        /// The value is logged here so integration tests can capture it; a production
        /// gateway would persist it alongside the bookmark for correlation.
        /// </remarks>
        public Task<Guid> StartExperimentAsync(
            Guid workflowInstanceId,
            Guid workflowNodeId,
            CancellationToken cancellationToken)
        {
            // Generate a deterministic fake DeviceJobId for local testing.
            // In a real gateway this comes from the device controller response.
            var deviceJobId = Guid.NewGuid();

            // In production you would make an HTTP/MQTT call here, e.g.:
            //   var response = await _httpClient.PostAsJsonAsync(
            //       "/api/jobs",
            //       new { workflowInstanceId, workflowNodeId },
            //       cancellationToken);
            //   deviceJobId = await response.Content.ReadFromJsonAsync<Guid>();

            return Task.FromResult(deviceJobId);
        }
    }
}
