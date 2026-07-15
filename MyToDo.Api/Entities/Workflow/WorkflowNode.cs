namespace MyToDo.Api.Entities.Workflow
{
    public class WorkflowNode
    {
        public Guid Id { get; set; }
        public Guid WorkflowVersionId { get; set; }
        public string NodeKey { get; set; } = string.Empty;
        public WorkflowNodeType NodeType { get; set; }
        public string? RequiredResourceType { get; set; }
        public int EstimatedDurationMinutes { get; set; } = 60;

        public WorkflowVersion? WorkflowVersion { get; set; }
        public ICollection<WorkflowEdge> OutgoingEdges { get; set; } = new List<WorkflowEdge>();
        public ICollection<WorkflowEdge> IncomingEdges { get; set; } = new List<WorkflowEdge>();
    }
}
