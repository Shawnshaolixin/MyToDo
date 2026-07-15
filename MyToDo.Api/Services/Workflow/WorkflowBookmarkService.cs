using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Persistent implementation of <see cref="IWorkflowBookmarkService"/> backed by EF Core.
    ///
    /// Concurrency considerations:
    ///   SQLite serialises all writes, so the optimistic check "Status == Active" before
    ///   marking a bookmark as Consumed is sufficient for local/development use.
    ///   For SQL Server / PostgreSQL in production, wrap ConsumeAsync in a serialisable
    ///   transaction or add a RowVersion stamp to WorkflowBookmark to prevent two concurrent
    ///   callers from both consuming the same bookmark.
    /// </summary>
    public class WorkflowBookmarkService : IWorkflowBookmarkService
    {
        private readonly MyToDoContext _context;

        public WorkflowBookmarkService(MyToDoContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<WorkflowBookmark> CreateAsync(
            Guid workflowInstanceId,
            Guid executionTokenId,
            Guid workflowNodeInstanceId,
            string bookmarkType,
            string bookmarkKey,
            CancellationToken cancellationToken)
        {
            var bookmark = new WorkflowBookmark
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                ExecutionTokenId = executionTokenId,
                WorkflowNodeInstanceId = workflowNodeInstanceId,
                BookmarkType = bookmarkType,
                BookmarkKey = bookmarkKey,
                Status = WorkflowBookmarkStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkflowBookmarks.Add(bookmark);
            await _context.SaveChangesAsync(cancellationToken);
            return bookmark;
        }

        /// <inheritdoc/>
        public Task<WorkflowBookmark?> FindAsync(
            string bookmarkType,
            string bookmarkKey,
            CancellationToken cancellationToken)
        {
            return _context.WorkflowBookmarks
                .FirstOrDefaultAsync(
                    b => b.BookmarkType == bookmarkType
                      && b.BookmarkKey == bookmarkKey
                      && b.Status == WorkflowBookmarkStatus.Active,
                    cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> ConsumeAsync(Guid bookmarkId, CancellationToken cancellationToken)
        {
            // Re-fetch inside a save boundary so that the status check and the update
            // are performed within the same DbContext change-unit.
            var bookmark = await _context.WorkflowBookmarks
                .FirstOrDefaultAsync(b => b.Id == bookmarkId && b.Status == WorkflowBookmarkStatus.Active, cancellationToken);

            if (bookmark == null)
            {
                // Already consumed or does not exist — idempotent.
                return false;
            }

            bookmark.Status = WorkflowBookmarkStatus.Consumed;
            bookmark.ConsumedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
