using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class WorkflowBookmarkService : IWorkflowBookmarkService
    {
        private readonly MyToDoContext _context;

        public WorkflowBookmarkService(MyToDoContext context)
        {
            _context = context;
        }

        public WorkflowBookmark Create(Guid workflowInstanceId, Guid executionTokenId, Guid workflowNodeInstanceId,
            string bookmarkType, string bookmarkKey)
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
            return bookmark;
        }

        public async Task<WorkflowBookmark?> FindAsync(string bookmarkType, string bookmarkKey, CancellationToken cancellationToken)
        {
            return await _context.WorkflowBookmarks
                .FirstOrDefaultAsync(x =>
                    x.BookmarkType == bookmarkType &&
                    x.BookmarkKey == bookmarkKey &&
                    x.Status == WorkflowBookmarkStatus.Active,
                    cancellationToken);
        }

        public void Consume(WorkflowBookmark bookmark)
        {
            bookmark.Status = WorkflowBookmarkStatus.Consumed;
            bookmark.ConsumedAt = DateTime.UtcNow;
        }
    }
}
