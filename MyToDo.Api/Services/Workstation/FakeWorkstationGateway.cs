namespace MyToDo.Api.Services.Workstation
{
    /// <summary>
    /// A deterministic fake implementation of <see cref="IWorkstationGateway"/> for
    /// local development and testing.  Register this as the default implementation
    /// in development; swap for a real HTTP-based gateway in production.
    ///
    /// Known limitations:
    ///   - DeviceJobId is a new Guid each call (not stable across restarts).
    ///   - No actual device communication occurs.
    ///   - All experiments and parameters are hard-coded.
    /// </summary>
    public class FakeWorkstationGateway : IWorkstationGateway
    {
        private static readonly IReadOnlyList<ExperimentDto> FakeExperiments =
        [
            new ExperimentDto("EXP-001", "Dissolution Test", "Standard USP dissolution assay"),
            new ExperimentDto("EXP-002", "HPLC Purity Analysis", "High-performance liquid chromatography purity run"),
            new ExperimentDto("EXP-003", "Viscosity Measurement", "Rotational viscometer measurement")
        ];

        private static readonly IReadOnlyList<ExperimentParameterSchemaDto> FakeParameters =
        [
            new ExperimentParameterSchemaDto("sampleId",     "Sample ID",         "string",  true,  null),
            new ExperimentParameterSchemaDto("temperature",  "Temperature (°C)",  "number",  false, "37"),
            new ExperimentParameterSchemaDto("duration",     "Duration (min)",    "number",  false, "30"),
            new ExperimentParameterSchemaDto("notes",        "Notes",             "string",  false, null)
        ];

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExperimentDto>> GetExperimentsAsync(
            string workstationCode,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(FakeExperiments);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExperimentParameterSchemaDto>> GetExperimentParametersAsync(
            string workstationCode,
            string experimentId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(FakeParameters);
        }

        /// <inheritdoc/>
        public Task<StartExperimentResult> StartExperimentAsync(
            string workstationCode,
            string experimentId,
            string parametersJson,
            CancellationToken cancellationToken)
        {
            var jobId = Guid.NewGuid().ToString();
            return Task.FromResult(new StartExperimentResult(Success: true, DeviceJobId: jobId, ErrorMessage: null));
        }

        /// <inheritdoc/>
        public Task<CommandResult> ApplyResolutionAsync(
            string workstationCode,
            string deviceJobId,
            string promptCode,
            string resolution,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new CommandResult(Success: true, ErrorMessage: null));
        }
    }
}
