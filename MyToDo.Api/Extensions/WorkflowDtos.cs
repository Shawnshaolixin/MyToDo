namespace MyToDo.Api.Extensions
{
    public class BootstrapWorkflowRequest
    {
        public string WorkflowName { get; set; } = "Demo Workflow";
        public string WorkOrderNo { get; set; } = string.Empty;
        public int Priority { get; set; } = 1;
        public string RequiredResourceType { get; set; } = "Workstation";
        public int EstimatedDurationMinutes { get; set; } = 60;
    }

    public class StartWorkflowRequest
    {
        public Guid WorkOrderId { get; set; }
        public Guid WorkflowVersionId { get; set; }
    }

    public class ResumeBookmarkRequest
    {
        public string BookmarkType { get; set; } = string.Empty;
        public string BookmarkKey { get; set; } = string.Empty;
        public string? Input { get; set; }
    }
}
