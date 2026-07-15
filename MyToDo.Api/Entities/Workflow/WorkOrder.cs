namespace MyToDo.Api.Entities.Workflow
{
    public class WorkOrder
    {
        public Guid Id { get; set; }
        public string WorkOrderNo { get; set; } = string.Empty;
        public Guid WorkflowVersionId { get; set; }
        public int Priority { get; set; } = 1;
        public DateTime EarliestStartTime { get; set; }
        public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public WorkflowVersion? WorkflowVersion { get; set; }
        public ICollection<WorkflowInstance> WorkflowInstances { get; set; } = new List<WorkflowInstance>();
    }
}
