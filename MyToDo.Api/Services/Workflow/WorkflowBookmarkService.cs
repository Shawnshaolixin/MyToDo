using System.Text.Json;
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

        public async Task<WorkflowBookmark> CreateAsync(
            Guid workflowInstanceId,
            Guid executionTokenId,
            Guid workflowNodeInstanceId,
            string bookmarkType,
            string bookmarkKey,
            object? input,
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
                InputJson = input == null ? null : JsonSerializer.Serialize(input),
                Status = WorkflowBookmarkStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkflowBookmarks.Add(bookmark);
            await _context.SaveChangesAsync(cancellationToken);
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

        public async Task ConsumeAsync(WorkflowBookmark bookmark, object? input, CancellationToken cancellationToken)
        {
            bookmark.Status = WorkflowBookmarkStatus.Consumed;
            bookmark.ConsumedAt = DateTime.UtcNow;
            bookmark.InputJson = input == null ? bookmark.InputJson : JsonSerializer.Serialize(input);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
