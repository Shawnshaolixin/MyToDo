namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Manages workflow bookmarks — persistent suspension points that allow
    /// a workflow instance to pause and resume at the same node when an
    /// external event (e.g. scheduling, device completion) occurs.
    /// </summary>
    public interface IWorkflowBookmarkService
    {
        /// <summary>
        /// Persists a new active bookmark.
        /// </summary>
        Task<Entities.Workflow.WorkflowBookmark> CreateAsync(
            Guid workflowInstanceId,
            Guid executionTokenId,
            Guid workflowNodeInstanceId,
            string bookmarkType,
            string bookmarkKey,
            CancellationToken cancellationToken);

        /// <summary>
        /// Finds the first active bookmark matching the given type and key.
        /// Returns <c>null</c> when no matching active bookmark exists.
        /// </summary>
        Task<Entities.Workflow.WorkflowBookmark?> FindAsync(
            string bookmarkType,
            string bookmarkKey,
            CancellationToken cancellationToken);

        /// <summary>
        /// Marks the bookmark as consumed and saves the change atomically.
        ///
        /// Concurrency note: Because SQLite serialises writes, a simple
        /// optimistic-concurrency check (comparing Status == Active before update)
        /// is sufficient for local/dev use.  In a high-concurrency SQL Server
        /// deployment you should add a <c>RowVersion</c> / <c>xact_abort</c> guard
        /// or use SELECT … FOR UPDATE to prevent double-consumption.
        /// </summary>
        Task<bool> ConsumeAsync(
            Guid bookmarkId,
            CancellationToken cancellationToken);
    }
}
