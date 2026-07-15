using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// 管理流程等待点的持久化状态。
    /// 创建书签表示流程挂起；消费书签表示外部条件已满足，流程可以继续推进。
    /// </summary>
    public class WorkflowBookmarkService : IWorkflowBookmarkService
    {
        private readonly MyToDoContext _context;
        private readonly ILogger<WorkflowBookmarkService> _logger;

        public WorkflowBookmarkService(MyToDoContext context, ILogger<WorkflowBookmarkService> logger)
        {
            _context = context;
            _logger = logger;
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

            _logger.LogInformation(
                "Creating workflow bookmark. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeInstanceId={WorkflowNodeInstanceId}, BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}",
                workflowInstanceId,
                executionTokenId,
                workflowNodeInstanceId,
                bookmarkType,
                bookmarkKey);

            _context.WorkflowBookmarks.Add(bookmark);
            await _context.SaveChangesAsync(cancellationToken);
            return bookmark;
        }

        public async Task<WorkflowBookmark?> FindAsync(string bookmarkType, string bookmarkKey, CancellationToken cancellationToken)
        {
            var bookmark = await _context.WorkflowBookmarks
                .FirstOrDefaultAsync(x =>
                    x.BookmarkType == bookmarkType &&
                    x.BookmarkKey == bookmarkKey &&
                    x.Status == WorkflowBookmarkStatus.Active,
                    cancellationToken);

            _logger.LogInformation(
                "Workflow bookmark lookup finished. Found={Found}",
                bookmark != null);

            return bookmark;
        }

        public async Task ConsumeAsync(WorkflowBookmark bookmark, object? input, CancellationToken cancellationToken)
        {
            // 消费书签意味着等待中的外部事件已经到达，该等待点不应再被重复恢复。
            bookmark.Status = WorkflowBookmarkStatus.Consumed;
            bookmark.ConsumedAt = DateTime.UtcNow;
            bookmark.InputJson = input == null ? bookmark.InputJson : JsonSerializer.Serialize(input);

            _logger.LogInformation(
                "Consuming workflow bookmark. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeInstanceId={WorkflowNodeInstanceId}, BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}",
                bookmark.WorkflowInstanceId,
                bookmark.ExecutionTokenId,
                bookmark.WorkflowNodeInstanceId,
                bookmark.BookmarkType,
                bookmark.BookmarkKey);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
