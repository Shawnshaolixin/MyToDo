namespace MyToDo.Api.Entities.Workflow
{
    public class SchedulableTask
    {
        public Guid Id { get; set; }
        public Guid WorkOrderId { get; set; }
        public Guid WorkflowInstanceId { get; set; }
        public Guid WorkflowNodeInstanceId { get; set; }
        public string RequiredResourceType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public DateTime EarliestStartTime { get; set; }
        public int DurationMinutes { get; set; } = 60;
        public SchedulableTaskStatus Status { get; set; } = SchedulableTaskStatus.ReadyForScheduling;
        public Guid? ScheduledResourceId { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
        public DateTime? ScheduledEndTime { get; set; }

        public WorkOrder? WorkOrder { get; set; }
        public WorkflowInstance? WorkflowInstance { get; set; }
        public WorkflowNodeInstance? WorkflowNodeInstance { get; set; }
        public SchedulingResource? ScheduledResource { get; set; }
        public ICollection<ScheduleResult> ScheduleResults { get; set; } = new List<ScheduleResult>();
    }
}
