namespace MyToDo.Api.Entities.Workflow
{
    public class WorkflowEdge
    {
        public Guid Id { get; set; }
        public Guid WorkflowVersionId { get; set; }
        public Guid FromNodeId { get; set; }
        public Guid ToNodeId { get; set; }

        public WorkflowVersion? WorkflowVersion { get; set; }
        public WorkflowNode? FromNode { get; set; }
        public WorkflowNode? ToNode { get; set; }
    }
}
