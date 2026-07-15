namespace MyToDo.Api.Services.Workflow
{
    public class StartExperimentResult
    {
        public bool Success { get; init; }
        public string DeviceJobId { get; init; } = string.Empty;
    }

    /// <summary>
    /// Abstraction for communicating with a physical or virtual workstation device.
    /// </summary>
    public interface IWorkstationGateway
    {
        /// <summary>
        /// Instructs the workstation to begin processing for the given resource type.
        /// Returns a <see cref="StartExperimentResult"/> with a device-issued job identifier.
        /// </summary>
        Task<StartExperimentResult> StartExperimentAsync(string resourceType, CancellationToken cancellationToken);
    }
}
