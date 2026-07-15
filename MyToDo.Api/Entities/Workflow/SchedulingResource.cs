namespace MyToDo.Api.Entities.Workflow
{
    public class SchedulingResource
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
    }
}
