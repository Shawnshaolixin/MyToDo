namespace MyToDo.Api.Services.Workflow
{
    public interface IWorkstationGateway
    {
        Task<IReadOnlyList<string>> GetExperimentsAsync(CancellationToken cancellationToken);
        Task<IReadOnlyDictionary<string, string>> GetExperimentParametersAsync(string experimentCode, CancellationToken cancellationToken);
        Task<StartExperimentResult> StartExperimentAsync(string experimentCode, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken);
        Task<bool> TriggerEventAsync(Guid deviceJobId, string eventCode, CancellationToken cancellationToken);
    }
}
