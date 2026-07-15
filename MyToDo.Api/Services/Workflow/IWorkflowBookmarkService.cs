using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Manages workflow bookmarks — suspension/resumption points that pause
    /// workflow execution until an external event (e.g. APS scheduling, device
    /// completion) fires and calls <see cref="IWorkflowRuntime.ResumeAsync"/>.
    ///
    /// Concurrency note: in high-throughput scenarios a distributed lock or a
    /// database-level unique index on (BookmarkType, BookmarkKey, Status=Active)
    /// should be used to prevent two concurrent threads consuming the same bookmark.
    /// For this minimal implementation the SQLite serialised write model is sufficient.
    /// </summary>
    public interface IWorkflowBookmarkService
    {
        /// <summary>
        /// Stages <paramref name="bookmark"/> for insertion and persists all pending
        /// EF change-tracker entries in one <c>SaveChangesAsync</c> call.
        /// This allows node executors to stage additional entity changes (e.g. a new
        /// <see cref="SchedulableTask"/>) before calling CreateAsync so everything
        /// is written atomically.
        /// </summary>
        Task<WorkflowBookmark> CreateAsync(WorkflowBookmark bookmark, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the first <see cref="WorkflowBookmarkStatus.Active"/> bookmark
        /// whose <see cref="WorkflowBookmark.BookmarkType"/> and
        /// <see cref="WorkflowBookmark.BookmarkKey"/> match, or <c>null</c> if none found.
        /// </summary>
        Task<WorkflowBookmark?> FindAsync(string bookmarkType, string bookmarkKey, CancellationToken cancellationToken);

        /// <summary>
        /// Marks <paramref name="bookmark"/> as <see cref="WorkflowBookmarkStatus.Consumed"/>
        /// and sets <see cref="WorkflowBookmark.ConsumedAt"/>.
        /// Does <b>not</b> call SaveChangesAsync — the caller (<see cref="IWorkflowRuntime"/>)
        /// is responsible for batching this mutation with other state changes and saving once.
        /// </summary>
        void Consume(WorkflowBookmark bookmark);
    }
}
