namespace MyToDo.Api.Services.Workflow
{
    public class NodeExecutionResult
    {
        public NodeExecutionOutcome Outcome { get; set; }
        public string? BookmarkType { get; set; }
        public string? BookmarkKey { get; set; }
        public string? InputJson { get; set; }

        public static NodeExecutionResult Done() => new() { Outcome = NodeExecutionOutcome.Done };

        public static NodeExecutionResult Waiting(string bookmarkType, string bookmarkKey, string? inputJson = null) =>
            new()
            {
                Outcome = NodeExecutionOutcome.Waiting,
                BookmarkType = bookmarkType,
                BookmarkKey = bookmarkKey,
                InputJson = inputJson
            };

        public static NodeExecutionResult Failed() => new() { Outcome = NodeExecutionOutcome.Failed };
    }
}
