using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// EF Core / SQLite-backed implementation of <see cref="IWorkflowBookmarkService"/>.
    ///
    /// All writes use the shared <see cref="MyToDoContext"/> (scoped lifetime) so they
    /// participate in the same unit-of-work as the calling <see cref="WorkflowRuntime"/>.
    /// </summary>
    public class WorkflowBookmarkService : IWorkflowBookmarkService
    {
        private readonly MyToDoContext _context;

        public WorkflowBookmarkService(MyToDoContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Any other tracked-but-unsaved changes (e.g. a <see cref="SchedulableTask"/> staged
        /// by the calling executor) are also persisted in the same transaction.
        /// </remarks>
        public async Task<WorkflowBookmark> CreateAsync(WorkflowBookmark bookmark, CancellationToken cancellationToken)
        {
            // Stage the bookmark for insertion
            _context.WorkflowBookmarks.Add(bookmark);

            // Flush the entire change tracker atomically so that the bookmark
            // and any co-staged entities (e.g. SchedulableTask) are written together.
            await _context.SaveChangesAsync(cancellationToken);

            return bookmark;
        }

        /// <inheritdoc/>
        public Task<WorkflowBookmark?> FindAsync(
            string bookmarkType,
            string bookmarkKey,
            CancellationToken cancellationToken)
        {
            // Index on (BookmarkType, BookmarkKey, Status) makes this lookup fast.
            return _context.WorkflowBookmarks
                .FirstOrDefaultAsync(
                    x => x.BookmarkType == bookmarkType &&
                         x.BookmarkKey == bookmarkKey &&
                         x.Status == WorkflowBookmarkStatus.Active,
                    cancellationToken);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Caller must invoke <c>SaveChangesAsync</c> after all related mutations
        /// (token status, node instance status, etc.) are complete to ensure an
        /// atomic write.
        /// </remarks>
        public void Consume(WorkflowBookmark bookmark)
        {
            // Mark as consumed so it cannot be resumed a second time
            bookmark.Status = WorkflowBookmarkStatus.Consumed;
            bookmark.ConsumedAt = DateTime.UtcNow;
        }
    }
}
