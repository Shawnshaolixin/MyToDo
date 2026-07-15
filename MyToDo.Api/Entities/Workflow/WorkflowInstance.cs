namespace MyToDo.Api.Entities.Workflow
{
    public class WorkflowInstance
    {
        public Guid Id { get; set; }
        public Guid WorkOrderId { get; set; }
        public Guid WorkflowVersionId { get; set; }
        public WorkflowInstanceStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public WorkOrder? WorkOrder { get; set; }
        public WorkflowVersion? WorkflowVersion { get; set; }
        public ICollection<WorkflowExecutionToken> ExecutionTokens { get; set; } = new List<WorkflowExecutionToken>();
        public ICollection<WorkflowNodeInstance> NodeInstances { get; set; } = new List<WorkflowNodeInstance>();
        public ICollection<WorkflowBookmark> Bookmarks { get; set; } = new List<WorkflowBookmark>();
    }
}
