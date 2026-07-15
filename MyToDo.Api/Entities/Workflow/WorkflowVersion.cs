namespace MyToDo.Api.Entities.Workflow
{
    public class WorkflowVersion
    {
        public Guid Id { get; set; }
        public Guid WorkflowId { get; set; }
        public int VersionNumber { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }

        public Workflow? Workflow { get; set; }
        public ICollection<WorkflowNode> Nodes { get; set; } = new List<WorkflowNode>();
        public ICollection<WorkflowEdge> Edges { get; set; } = new List<WorkflowEdge>();
    }
}
