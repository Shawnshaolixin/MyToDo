using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Executor for <see cref="WorkflowNodeType.ScheduleTask"/> nodes.
    ///
    /// When the workflow reaches a ScheduleTask node this executor:
    /// <list type="number">
    ///   <item>Creates a <see cref="SchedulableTask"/> record so the APS scheduler can
    ///         pick it up on the next scheduling run.</item>
    ///   <item>Creates a <see cref="WorkflowBookmark"/> of type
    ///         <see cref="WorkflowBookmarkTypes.ScheduleTaskScheduled"/> keyed by the
    ///         schedulable task ID, then persists both atomically via
    ///         <see cref="IWorkflowBookmarkService.CreateAsync"/>.</item>
    ///   <item>Returns <see cref="NodeExecutionOutcome.Waiting"/> so the runtime
    ///         suspends the workflow.</item>
    /// </list>
    /// The workflow resumes when <see cref="IApsScheduler.ScheduleAsync"/> allocates a
    /// resource and <see cref="IWorkflowRuntime.ResumeAsync"/> is called with the task ID
    /// as the bookmark key.
    /// </summary>
    public class ScheduleTaskExecutor : IWorkflowNodeExecutor
    {
        private readonly MyToDoContext _context;
        private readonly IWorkflowBookmarkService _bookmarkService;

        public ScheduleTaskExecutor(MyToDoContext context, IWorkflowBookmarkService bookmarkService)
        {
            _context = context;
            _bookmarkService = bookmarkService;
        }

        /// <inheritdoc/>
        public WorkflowNodeType NodeType => WorkflowNodeType.ScheduleTask;

        /// <inheritdoc/>
        public async Task<NodeExecutionResult> ExecuteAsync(
            NodeExecutionContext context,
            CancellationToken cancellationToken)
        {
            // Load the work order to obtain scheduling parameters (priority, earliest start)
            var workOrder = await _context.WorkOrders
                .FirstAsync(x => x.Id == context.Instance.WorkOrderId, cancellationToken);

            // Create a schedulable task entry for the APS scheduler to process on its next run
            var schedulableTask = new SchedulableTask
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrder.Id,
                WorkflowInstanceId = context.Instance.Id,
                WorkflowNodeInstanceId = context.NodeInstance.Id,
                RequiredResourceType = context.Node.RequiredResourceType ?? "Workstation",
                Priority = workOrder.Priority,
                EarliestStartTime = workOrder.EarliestStartTime,
                DurationMinutes = context.Node.EstimatedDurationMinutes,
                Status = SchedulableTaskStatus.ReadyForScheduling
            };
            // Stage the task — will be written by the bookmark CreateAsync call below
            _context.SchedulableTasks.Add(schedulableTask);

            // Suspend execution and wait for the APS scheduler to allocate resources.
            // Using the schedulable task ID as the bookmark key lets ResumeAsync
            // validate that the task is actually scheduled before resuming.
            var bookmark = new WorkflowBookmark
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = context.Instance.Id,
                ExecutionTokenId = context.Token.Id,
                WorkflowNodeInstanceId = context.NodeInstance.Id,
                BookmarkType = WorkflowBookmarkTypes.ScheduleTaskScheduled,
                BookmarkKey = schedulableTask.Id.ToString(),
                Status = WorkflowBookmarkStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            // Update status before persisting so the atomic write captures everything
            context.NodeInstance.Status = WorkflowNodeInstanceStatus.Waiting;
            context.Token.Status = WorkflowExecutionTokenStatus.Waiting;
            context.Token.UpdatedAt = DateTime.UtcNow;
            context.Instance.Status = WorkflowInstanceStatus.Suspended;

            // CreateAsync flushes all staged changes (task + bookmark + status updates) at once
            await _bookmarkService.CreateAsync(bookmark, cancellationToken);

            return NodeExecutionResult.Waiting();
        }
    }
}
