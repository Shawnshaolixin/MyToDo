namespace MyToDo.Api.Services.Workstation
{
    // ── Contracts used by IWorkstationGateway ─────────────────────────────────

    /// <summary>Represents an experiment definition available on a workstation.</summary>
    public record ExperimentDto(string Id, string Name, string? Description);

    /// <summary>Describes a single parameter field for an experiment.</summary>
    public record ExperimentParameterSchemaDto(
        string ParameterKey,
        string Label,
        string Type,           // "string" | "number" | "boolean"
        bool Required,
        string? DefaultValue);

    /// <summary>Result returned by StartExperiment.</summary>
    public record StartExperimentResult(bool Success, string? DeviceJobId, string? ErrorMessage);

    /// <summary>Result returned by ApplyResolution.</summary>
    public record CommandResult(bool Success, string? ErrorMessage);

    // ── Gateway interface ─────────────────────────────────────────────────────

    /// <summary>
    /// Abstraction over the physical workstation device API.
    /// Concrete implementations include a real HTTP client and
    /// <see cref="FakeWorkstationGateway"/> for local development.
    /// </summary>
    public interface IWorkstationGateway
    {
        /// <summary>Returns the list of experiment definitions available on the workstation.</summary>
        Task<IReadOnlyList<ExperimentDto>> GetExperimentsAsync(string workstationCode, CancellationToken cancellationToken);

        /// <summary>Returns the parameter schema for a specific experiment.</summary>
        Task<IReadOnlyList<ExperimentParameterSchemaDto>> GetExperimentParametersAsync(
            string workstationCode,
            string experimentId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Instructs the device to start an experiment.
        /// On success the device returns a <c>DeviceJobId</c> that will be echoed
        /// back in subsequent event callbacks.
        /// </summary>
        Task<StartExperimentResult> StartExperimentAsync(
            string workstationCode,
            string experimentId,
            string parametersJson,
            CancellationToken cancellationToken);

        /// <summary>Sends an operator resolution (answer to a prompt) to the device.</summary>
        Task<CommandResult> ApplyResolutionAsync(
            string workstationCode,
            string deviceJobId,
            string promptCode,
            string resolution,
            CancellationToken cancellationToken);
    }
}
