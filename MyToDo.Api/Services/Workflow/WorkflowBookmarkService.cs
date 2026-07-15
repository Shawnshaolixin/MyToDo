using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// EF Core implementation for bookmark persistence.
    /// 
    /// Concurrency note:
    /// We do a status check before consuming a bookmark (must be Active).
    /// In high-contention scenarios, callers should still treat duplicate resume attempts as possible
    /// and rely on idempotent resume handling if they run in multiple processes.
    /// </summary>
    public class WorkflowBookmarkService : IWorkflowBookmarkService
    {
        private readonly MyToDoContext _context;

        public WorkflowBookmarkService(MyToDoContext context)
        {
            _context = context;
        }

        public async Task<WorkflowBookmark> CreateAsync(WorkflowBookmark bookmark, CancellationToken cancellationToken)
        {
            _context.WorkflowBookmarks.Add(bookmark);
            await _context.SaveChangesAsync(cancellationToken);
            return bookmark;
        }

        public Task<WorkflowBookmark?> FindAsync(string bookmarkType, string bookmarkKey, CancellationToken cancellationToken)
        {
            return _context.WorkflowBookmarks
                .FirstOrDefaultAsync(
                    x => x.BookmarkType == bookmarkType &&
                         x.BookmarkKey == bookmarkKey &&
                         x.Status == WorkflowBookmarkStatus.Active,
                    cancellationToken);
        }

        public async Task<bool> ConsumeAsync(Guid bookmarkId, CancellationToken cancellationToken)
        {
            var bookmark = await _context.WorkflowBookmarks
                .FirstOrDefaultAsync(x => x.Id == bookmarkId, cancellationToken);

            if (bookmark == null || bookmark.Status != WorkflowBookmarkStatus.Active)
            {
                return false;
            }

            bookmark.Status = WorkflowBookmarkStatus.Consumed;
            bookmark.ConsumedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
