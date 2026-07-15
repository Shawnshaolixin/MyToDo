namespace MyToDo.Api.Entities.Workflow
{
    public class Workflow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ICollection<WorkflowVersion> Versions { get; set; } = new List<WorkflowVersion>();
    }
}
