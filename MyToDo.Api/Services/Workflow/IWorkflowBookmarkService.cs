using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Manages workflow bookmarks used to suspend and resume token execution.
    /// </summary>
    public interface IWorkflowBookmarkService
    {
        Task<WorkflowBookmark> CreateAsync(WorkflowBookmark bookmark, CancellationToken cancellationToken);
        Task<WorkflowBookmark?> FindAsync(string bookmarkType, string bookmarkKey, CancellationToken cancellationToken);
        Task<bool> ConsumeAsync(Guid bookmarkId, CancellationToken cancellationToken);
    }
}
