namespace MyToDo.Api.Entities.Workflow
{
    public class WorkflowBookmark
    {
        public Guid Id { get; set; }
        public Guid WorkflowInstanceId { get; set; }
        public Guid ExecutionTokenId { get; set; }
        public Guid WorkflowNodeInstanceId { get; set; }
        public string BookmarkType { get; set; } = string.Empty;
        public string BookmarkKey { get; set; } = string.Empty;
        public string? InputJson { get; set; }
        public WorkflowBookmarkStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConsumedAt { get; set; }

        public WorkflowInstance? WorkflowInstance { get; set; }
    }
}
