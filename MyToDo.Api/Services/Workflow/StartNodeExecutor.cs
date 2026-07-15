using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Executor for <see cref="WorkflowNodeType.Start"/> nodes.
    ///
    /// A Start node is a pure pass-through: it has no side-effects and immediately
    /// signals <see cref="NodeExecutionOutcome.Done"/> so the runtime advances the
    /// execution token to the first downstream node.
    /// </summary>
    public class StartNodeExecutor : IWorkflowNodeExecutor
    {
        /// <inheritdoc/>
        public WorkflowNodeType NodeType => WorkflowNodeType.Start;

        /// <inheritdoc/>
        public Task<NodeExecutionResult> ExecuteAsync(
            NodeExecutionContext context,
            CancellationToken cancellationToken)
        {
            // Complete the node instance record immediately — no external work required
            context.NodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
            context.NodeInstance.CompletedAt = DateTime.UtcNow;

            // Return Done: runtime will move token to the next node and continue looping
            return Task.FromResult(NodeExecutionResult.Done());
        }
    }
}
