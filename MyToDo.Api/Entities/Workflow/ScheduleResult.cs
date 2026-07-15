namespace MyToDo.Api.Entities.Workflow
{
    public class ScheduleResult
    {
        public Guid Id { get; set; }
        public Guid SchedulableTaskId { get; set; }
        public Guid ResourceId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CreatedAt { get; set; }

        public SchedulableTask? SchedulableTask { get; set; }
        public SchedulingResource? Resource { get; set; }
    }
}
