using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// 负责管理流程挂起点（Bookmark），用于把外部事件与等待中的流程节点关联起来。
    /// </summary>
    public interface IWorkflowBookmarkService
    {
        /// <summary>
        /// 为等待中的节点创建可恢复书签，并记录恢复时需要关联的上下文信息。
        /// </summary>
        Task<WorkflowBookmark> CreateAsync(
            Guid workflowInstanceId,
            Guid executionTokenId,
            Guid workflowNodeInstanceId,
            string bookmarkType,
            string bookmarkKey,
            object? input,
            CancellationToken cancellationToken);

        /// <summary>
        /// 查找仍处于激活状态的书签；找不到时表示流程当前没有对应的等待点。
        /// </summary>
        Task<WorkflowBookmark?> FindAsync(string bookmarkType, string bookmarkKey, CancellationToken cancellationToken);

        /// <summary>
        /// 消费书签，表示外部事件已经到达，等待中的节点可以继续执行。
        /// </summary>
        Task ConsumeAsync(WorkflowBookmark bookmark, object? input, CancellationToken cancellationToken);
    }
}
