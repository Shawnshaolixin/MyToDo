namespace MyToDo.Api.Services.Workflow
{
    public interface IWorkstationGateway
    {
        Task<IReadOnlyList<WorkstationExperiment>> ListExperimentsAsync(CancellationToken cancellationToken);

        Task<WorkstationParameterSchema> GetParameterSchemaAsync(string experimentCode, CancellationToken cancellationToken);

        Task<StartExperimentResponse> StartExperimentAsync(StartExperimentRequest request, CancellationToken cancellationToken);

        Task<GatewayOperationResponse> ApplyResolutionAsync(string deviceJobId, string resolution, CancellationToken cancellationToken);
    }

    public record WorkstationExperiment(string ExperimentCode, string DisplayName);

    public record WorkstationParameterSchema(string ExperimentCode, IReadOnlyDictionary<string, string> Parameters);

    public record StartExperimentRequest(Guid WorkflowInstanceId, Guid WorkOrderId, string ExperimentCode);

    public record StartExperimentResponse(bool Success, string DeviceJobId);

    public record GatewayOperationResponse(bool Success);
}
