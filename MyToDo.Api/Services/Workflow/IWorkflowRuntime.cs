using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// 负责驱动流程实例在节点之间推进、挂起与恢复。
    /// </summary>
    public interface IWorkflowRuntime
    {
        /// <summary>
        /// 创建流程实例与初始执行令牌，并从开始节点持续推进，直到流程进入等待态或结束。
        /// </summary>
        Task<WorkflowInstance> StartAsync(Guid workOrderId, Guid workflowVersionId, CancellationToken cancellationToken);

        /// <summary>
        /// 根据书签恢复一个已挂起的流程节点，并继续向后推进。
        /// </summary>
        Task<bool> ResumeAsync(string bookmarkType, string bookmarkKey, object? input, CancellationToken cancellationToken);

        /// <summary>
        /// 执行当前令牌指向的节点，并根据执行结果更新节点、令牌和流程实例状态。
        /// </summary>
        Task ExecuteNodeAsync(Guid workflowInstanceId, Guid executionTokenId, CancellationToken cancellationToken);
    }
}
