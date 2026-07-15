namespace MyToDo.Api.Entities.Workflow
{
    public class WorkflowNodeInstance
    {
        public Guid Id { get; set; }
        public Guid WorkflowInstanceId { get; set; }
        public Guid WorkflowNodeId { get; set; }
        public Guid ExecutionTokenId { get; set; }
        public WorkflowNodeInstanceStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public WorkflowInstance? WorkflowInstance { get; set; }
    }
}
