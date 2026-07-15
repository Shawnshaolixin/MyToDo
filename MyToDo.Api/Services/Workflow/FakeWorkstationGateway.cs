namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Local in-process fake gateway used for development and testing.
    /// Always reports success and returns a generated device job ID.
    /// </summary>
    public class FakeWorkstationGateway : IWorkstationGateway
    {
        public Task<StartExperimentResult> StartExperimentAsync(string resourceType, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StartExperimentResult
            {
                Success = true,
                DeviceJobId = $"dev-{Guid.NewGuid():N}"
            });
        }
    }
}
