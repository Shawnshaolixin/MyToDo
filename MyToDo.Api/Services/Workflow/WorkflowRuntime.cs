using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// 负责驱动流程实例从开始节点持续向后执行。
    /// 当节点需要等待外部事件时挂起；当收到对应书签时再恢复执行。
    /// </summary>
    public class WorkflowRuntime : IWorkflowRuntime
    {
        private readonly MyToDoContext _context;
        private readonly IWorkflowNodeExecutorRegistry _executorRegistry;
        private readonly IWorkflowBookmarkService _bookmarkService;
        private readonly ILogger<WorkflowRuntime> _logger;

        public WorkflowRuntime(
            MyToDoContext context,
            IWorkflowNodeExecutorRegistry executorRegistry,
            IWorkflowBookmarkService bookmarkService,
            ILogger<WorkflowRuntime> logger)
        {
            _context = context;
            _executorRegistry = executorRegistry;
            _bookmarkService = bookmarkService;
            _logger = logger;
        }

        /// <summary>
        /// 初始化流程实例与首个执行令牌，并从开始节点自动推进到第一个等待点或结束点。
        /// </summary>
        public async Task<WorkflowInstance> StartAsync(Guid workOrderId, Guid workflowVersionId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starting workflow instance. WorkOrderId={WorkOrderId}, WorkflowVersionId={WorkflowVersionId}",
                workOrderId,
                workflowVersionId);

            var workOrder = await _context.WorkOrders.FirstOrDefaultAsync(x => x.Id == workOrderId, cancellationToken)
                ?? throw new InvalidOperationException("WorkOrder not found.");

            var startNode = await _context.WorkflowNodes
                .FirstOrDefaultAsync(x => x.WorkflowVersionId == workflowVersionId && x.NodeType == WorkflowNodeType.Start, cancellationToken)
                ?? throw new InvalidOperationException("Start node not found.");

            workOrder.Status = WorkOrderStatus.InProgress;
            workOrder.WorkflowVersionId = workflowVersionId;

            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrderId,
                WorkflowVersionId = workflowVersionId,
                Status = WorkflowInstanceStatus.Running,
                StartedAt = DateTime.UtcNow
            };

            var token = new WorkflowExecutionToken
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instance.Id,
                CurrentNodeId = startNode.Id,
                Status = WorkflowExecutionTokenStatus.Ready,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.WorkflowInstances.Add(instance);
            _context.WorkflowExecutionTokens.Add(token);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created workflow instance and initial token. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, WorkOrderId={WorkOrderId}",
                instance.Id,
                token.Id,
                startNode.Id,
                workOrderId);

            await AdvanceUntilPauseOrCompleteAsync(instance.Id, token.Id, cancellationToken);

            _logger.LogInformation(
                "Workflow start advance finished. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowStatus={WorkflowStatus}",
                instance.Id,
                token.Id,
                instance.Status);

            return instance;
        }

        /// <summary>
        /// 根据书签恢复等待中的节点。
        /// 恢复后会把令牌重新置为 Ready，并继续向下执行直到再次等待或结束。
        /// </summary>
        public async Task<bool> ResumeAsync(string bookmarkType, string bookmarkKey, object? input, CancellationToken cancellationToken)
        {
            var sanitizedBookmarkType = WorkflowLogSanitizer.Sanitize(bookmarkType);
            var sanitizedBookmarkKey = WorkflowLogSanitizer.Sanitize(bookmarkKey);

            _logger.LogInformation(
                "Attempting to resume workflow by bookmark. BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}",
                sanitizedBookmarkType,
                sanitizedBookmarkKey);

            var bookmark = await _bookmarkService.FindAsync(bookmarkType, bookmarkKey, cancellationToken);
            if (bookmark == null)
            {
                _logger.LogWarning(
                    "Workflow bookmark not found. BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}",
                    sanitizedBookmarkType,
                    sanitizedBookmarkKey);

                return false;
            }

            if (bookmarkType == WorkflowBookmarkTypes.ScheduleTaskScheduled && Guid.TryParse(bookmark.BookmarkKey, out var taskId))
            {
                var task = await _context.SchedulableTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
                if (task == null || task.Status != SchedulableTaskStatus.Scheduled)
                {
                    _logger.LogWarning(
                        "Schedulable task is not ready for workflow resume. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}, TaskStatus={TaskStatus}",
                        bookmark.WorkflowInstanceId,
                        bookmark.ExecutionTokenId,
                        sanitizedBookmarkType,
                        sanitizedBookmarkKey,
                        task?.Status);

                    return false;
                }
            }

            var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == bookmark.ExecutionTokenId, cancellationToken);
            var instance = await _context.WorkflowInstances.FirstAsync(x => x.Id == bookmark.WorkflowInstanceId, cancellationToken);
            var nodeInstance = await _context.WorkflowNodeInstances.FirstAsync(x => x.Id == bookmark.WorkflowNodeInstanceId, cancellationToken);

            await _bookmarkService.ConsumeAsync(bookmark, input, cancellationToken);

            // 书签被消费后，说明等待条件已经满足，节点可以标记完成，令牌重新回到 Ready。
            nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
            nodeInstance.CompletedAt = DateTime.UtcNow;
            token.Status = WorkflowExecutionTokenStatus.Ready;
            token.UpdatedAt = DateTime.UtcNow;
            instance.Status = WorkflowInstanceStatus.Running;

            await MoveTokenToNextNodeOrCompleteAsync(instance, token, nodeInstance.WorkflowNodeId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await AdvanceUntilPauseOrCompleteAsync(instance.Id, token.Id, cancellationToken);

            _logger.LogInformation(
                "Workflow resumed successfully. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}, WorkflowStatus={WorkflowStatus}",
                instance.Id,
                token.Id,
                nodeInstance.WorkflowNodeId,
                sanitizedBookmarkType,
                sanitizedBookmarkKey,
                instance.Status);

            return true;
        }

        /// <summary>
        /// 执行当前令牌所在节点，并根据执行器返回结果更新流程状态。
        /// Done 表示继续推进；Waiting 表示挂起并创建书签；Failed 表示流程失败。
        /// </summary>
        public async Task ExecuteNodeAsync(Guid workflowInstanceId, Guid executionTokenId, CancellationToken cancellationToken)
        {
            var instance = await _context.WorkflowInstances.FirstAsync(x => x.Id == workflowInstanceId, cancellationToken);
            var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == executionTokenId, cancellationToken);
            var workOrder = await _context.WorkOrders.FirstAsync(x => x.Id == instance.WorkOrderId, cancellationToken);

            if (token.Status != WorkflowExecutionTokenStatus.Ready)
            {
                _logger.LogInformation(
                    "Skipping workflow node execution because token is not ready. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, TokenStatus={TokenStatus}",
                    workflowInstanceId,
                    executionTokenId,
                    token.Status);

                return;
            }

            var node = await _context.WorkflowNodes.FirstOrDefaultAsync(x => x.Id == token.CurrentNodeId, cancellationToken)
                ?? throw new InvalidOperationException("Workflow node not found.");

            var nodeInstance = new WorkflowNodeInstance
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instance.Id,
                WorkflowNodeId = node.Id,
                ExecutionTokenId = token.Id,
                Status = WorkflowNodeInstanceStatus.Running,
                StartedAt = DateTime.UtcNow
            };

            _context.WorkflowNodeInstances.Add(nodeInstance);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Executing workflow node. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, NodeType={NodeType}, WorkOrderId={WorkOrderId}",
                instance.Id,
                token.Id,
                node.Id,
                node.NodeType,
                workOrder.Id);

            var executor = _executorRegistry.GetExecutor(node.NodeType);
            var executionResult = await executor.ExecuteAsync(
                new WorkflowNodeExecutionContext
                {
                    WorkflowInstance = instance,
                    ExecutionToken = token,
                    WorkflowNode = node,
                    WorkflowNodeInstance = nodeInstance,
                    WorkOrder = workOrder
                },
                cancellationToken);

            _logger.LogInformation(
                "Workflow node execution finished. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, NodeType={NodeType}, ExecutionStatus={ExecutionStatus}",
                instance.Id,
                token.Id,
                node.Id,
                node.NodeType,
                executionResult.Status);

            if (executionResult.Status == NodeExecutionStatus.Done)
            {
                nodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
                nodeInstance.CompletedAt = DateTime.UtcNow;

                if (token.Status != WorkflowExecutionTokenStatus.Completed)
                {
                    await MoveTokenToNextNodeOrCompleteAsync(instance, token, node.Id, cancellationToken);
                }
                else
                {
                    await CompleteWorkflowIfNoActiveTokensAsync(instance, token.Id, cancellationToken);
                }
            }
            else if (executionResult.Status == NodeExecutionStatus.Waiting)
            {
                if (string.IsNullOrWhiteSpace(executionResult.BookmarkType) || string.IsNullOrWhiteSpace(executionResult.BookmarkKey))
                {
                    _logger.LogError(
                        "Workflow node returned waiting status without bookmark. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, NodeType={NodeType}",
                        instance.Id,
                        token.Id,
                        node.Id,
                        node.NodeType);

                    throw new InvalidOperationException("Waiting result must include bookmark type and key.");
                }

                // Waiting 表示节点逻辑已经发起外部动作，但流程必须等待回调或外部事件才能继续。
                nodeInstance.Status = WorkflowNodeInstanceStatus.Waiting;
                token.Status = WorkflowExecutionTokenStatus.Waiting;
                token.UpdatedAt = DateTime.UtcNow;
                instance.Status = WorkflowInstanceStatus.Suspended;

                await _bookmarkService.CreateAsync(
                    instance.Id,
                    token.Id,
                    nodeInstance.Id,
                    executionResult.BookmarkType,
                    executionResult.BookmarkKey,
                    executionResult.BookmarkInput,
                    cancellationToken);

                _logger.LogInformation(
                    "Workflow node entered waiting state. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, NodeType={NodeType}, BookmarkType={BookmarkType}, BookmarkKey={BookmarkKey}",
                    instance.Id,
                    token.Id,
                    node.Id,
                    node.NodeType,
                    executionResult.BookmarkType,
                    executionResult.BookmarkKey);

                return;
            }
            else
            {
                _logger.LogError(
                    "Workflow node execution failed. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}, NodeType={NodeType}, ErrorMessage={ErrorMessage}",
                    instance.Id,
                    token.Id,
                    node.Id,
                    node.NodeType,
                    executionResult.ErrorMessage);

                nodeInstance.Status = WorkflowNodeInstanceStatus.Failed;
                nodeInstance.CompletedAt = DateTime.UtcNow;
                token.Status = WorkflowExecutionTokenStatus.Failed;
                token.UpdatedAt = DateTime.UtcNow;
                instance.Status = WorkflowInstanceStatus.Failed;
                workOrder.Status = WorkOrderStatus.Failed;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 连续执行 Ready 状态的令牌，直到流程进入 Waiting/Completed/Failed。
        /// 加入迭代保护，避免流程配置异常时发生无限循环。
        /// </summary>
        private async Task AdvanceUntilPauseOrCompleteAsync(Guid workflowInstanceId, Guid executionTokenId, CancellationToken cancellationToken)
        {
            var iteration = 0;
            while (true)
            {
                iteration++;
                if (iteration > 1000)
                {
                    _logger.LogError(
                        "Workflow execution exceeded max iteration guard. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, Iteration={Iteration}",
                        workflowInstanceId,
                        executionTokenId,
                        iteration);

                    throw new InvalidOperationException("Workflow execution exceeded max iteration guard.");
                }

                var token = await _context.WorkflowExecutionTokens.FirstAsync(x => x.Id == executionTokenId, cancellationToken);
                if (token.Status != WorkflowExecutionTokenStatus.Ready)
                {
                    _logger.LogInformation(
                        "Stopping workflow auto-advance because token is no longer ready. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, TokenStatus={TokenStatus}",
                        workflowInstanceId,
                        executionTokenId,
                        token.Status);

                    return;
                }

                await ExecuteNodeAsync(workflowInstanceId, executionTokenId, cancellationToken);
            }
        }

        /// <summary>
        /// 把令牌移动到下一个节点；如果已经没有后继边，则把当前令牌标记为完成。
        /// </summary>
        private async Task MoveTokenToNextNodeOrCompleteAsync(
            WorkflowInstance instance,
            WorkflowExecutionToken token,
            Guid currentNodeId,
            CancellationToken cancellationToken)
        {
            var nextEdge = await _context.WorkflowEdges
                .Where(x => x.WorkflowVersionId == instance.WorkflowVersionId && x.FromNodeId == currentNodeId)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextEdge == null)
            {
                _logger.LogInformation(
                    "No next workflow edge found; marking token completed. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, WorkflowNodeId={WorkflowNodeId}",
                    instance.Id,
                    token.Id,
                    currentNodeId);

                token.Status = WorkflowExecutionTokenStatus.Completed;
                token.UpdatedAt = DateTime.UtcNow;

                await CompleteWorkflowIfNoActiveTokensAsync(instance, token.Id, cancellationToken);
                return;
            }

            _logger.LogInformation(
                "Moving workflow token to next node. WorkflowInstanceId={WorkflowInstanceId}, ExecutionTokenId={ExecutionTokenId}, FromWorkflowNodeId={FromWorkflowNodeId}, ToWorkflowNodeId={ToWorkflowNodeId}",
                instance.Id,
                token.Id,
                currentNodeId,
                nextEdge.ToNodeId);

            token.CurrentNodeId = nextEdge.ToNodeId;
            token.Status = WorkflowExecutionTokenStatus.Ready;
            token.UpdatedAt = DateTime.UtcNow;
            instance.Status = WorkflowInstanceStatus.Running;
        }

        /// <summary>
        /// 当当前令牌完成后，检查是否仍有活跃令牌。
        /// 只有在所有令牌都不再运行或等待时，流程实例和工单才会真正完成。
        /// </summary>
        private async Task CompleteWorkflowIfNoActiveTokensAsync(
            WorkflowInstance instance,
            Guid completedTokenId,
            CancellationToken cancellationToken)
        {
            var hasActiveTokens = await _context.WorkflowExecutionTokens
                .AnyAsync(x =>
                    x.WorkflowInstanceId == instance.Id &&
                    x.Id != completedTokenId &&
                    (x.Status == WorkflowExecutionTokenStatus.Ready || x.Status == WorkflowExecutionTokenStatus.Waiting),
                    cancellationToken);

            if (hasActiveTokens)
            {
                _logger.LogInformation(
                    "Workflow still has active tokens; instance remains active. WorkflowInstanceId={WorkflowInstanceId}, CompletedExecutionTokenId={ExecutionTokenId}",
                    instance.Id,
                    completedTokenId);

                return;
            }

            instance.Status = WorkflowInstanceStatus.Completed;
            instance.CompletedAt = DateTime.UtcNow;

            var workOrder = await _context.WorkOrders.FirstAsync(x => x.Id == instance.WorkOrderId, cancellationToken);
            workOrder.Status = WorkOrderStatus.Completed;
            workOrder.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Workflow instance completed. WorkflowInstanceId={WorkflowInstanceId}, WorkOrderId={WorkOrderId}",
                instance.Id,
                instance.WorkOrderId);
        }
    }
}
