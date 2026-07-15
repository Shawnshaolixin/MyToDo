namespace MyToDo.Api.Services.Workflow
{
    public class FakeWorkstationGateway : IWorkstationGateway
    {
        public Task<IReadOnlyList<WorkstationExperiment>> ListExperimentsAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<WorkstationExperiment> experiments =
            [
                new WorkstationExperiment("EXP-DEMO-1", "Demo Experiment 1"),
                new WorkstationExperiment("EXP-DEMO-2", "Demo Experiment 2")
            ];

            return Task.FromResult(experiments);
        }

        public Task<WorkstationParameterSchema> GetParameterSchemaAsync(string experimentCode, CancellationToken cancellationToken)
        {
            var schema = new WorkstationParameterSchema(
                experimentCode,
                new Dictionary<string, string>
                {
                    ["temperature"] = "number",
                    ["durationMinutes"] = "integer"
                });

            return Task.FromResult(schema);
        }

        public Task<StartExperimentResponse> StartExperimentAsync(StartExperimentRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StartExperimentResponse(true, Guid.NewGuid().ToString("D")));
        }

        public Task<GatewayOperationResponse> ApplyResolutionAsync(string deviceJobId, string resolution, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GatewayOperationResponse(true));
        }
    }
}
