namespace MyToDo.Api.Services.Workflow
{
    public class StartExperimentResult
    {
        public Guid DeviceJobId { get; set; }
        public bool Accepted { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
