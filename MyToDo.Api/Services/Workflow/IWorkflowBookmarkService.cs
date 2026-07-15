using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public interface IWorkflowBookmarkService
    {
        Task<WorkflowBookmark> CreateAsync(
            Guid workflowInstanceId,
            Guid executionTokenId,
            Guid workflowNodeInstanceId,
            string bookmarkType,
            string bookmarkKey,
            object? input,
            CancellationToken cancellationToken);

        Task<WorkflowBookmark?> FindAsync(string bookmarkType, string bookmarkKey, CancellationToken cancellationToken);

        Task ConsumeAsync(WorkflowBookmark bookmark, object? input, CancellationToken cancellationToken);
    }
}
