namespace MyToDo.Api.Entities.Workflow
{
    public class WorkflowExecutionToken
    {
        public Guid Id { get; set; }
        public Guid WorkflowInstanceId { get; set; }
        public Guid CurrentNodeId { get; set; }
        public WorkflowExecutionTokenStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public WorkflowInstance? WorkflowInstance { get; set; }
    }
}
