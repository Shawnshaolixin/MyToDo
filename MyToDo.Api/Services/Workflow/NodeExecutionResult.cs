namespace MyToDo.Api.Services.Workflow
{
    public enum NodeExecutionStatus
    {
        Done = 0,
        Waiting = 1,
        Failed = 2
    }

    public class NodeExecutionResult
    {
        public NodeExecutionStatus Status { get; init; }
        public string? BookmarkType { get; init; }
        public string? BookmarkKey { get; init; }
        public object? BookmarkInput { get; init; }
        public string? ErrorMessage { get; init; }

        public static NodeExecutionResult Done() => new() { Status = NodeExecutionStatus.Done };

        public static NodeExecutionResult Waiting(string bookmarkType, string bookmarkKey, object? bookmarkInput = null)
        {
            return new NodeExecutionResult
            {
                Status = NodeExecutionStatus.Waiting,
                BookmarkType = bookmarkType,
                BookmarkKey = bookmarkKey,
                BookmarkInput = bookmarkInput
            };
        }

        public static NodeExecutionResult Failed(string errorMessage) => new() { Status = NodeExecutionStatus.Failed, ErrorMessage = errorMessage };
    }
}
