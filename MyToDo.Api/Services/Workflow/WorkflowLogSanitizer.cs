namespace MyToDo.Api.Services.Workflow
{
    internal static class WorkflowLogSanitizer
    {
        public static string? Sanitize(string? value)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value
                    .Replace("\r", "\\r", StringComparison.Ordinal)
                    .Replace("\n", "\\n", StringComparison.Ordinal);
        }
    }
}
