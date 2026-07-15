using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Executor for <see cref="WorkflowNodeType.End"/> nodes.
    ///
    /// Marks the current execution token as <see cref="WorkflowExecutionTokenStatus.Completed"/>
    /// and the workflow instance as <see cref="WorkflowInstanceStatus.Completed"/>.
    /// The runtime is responsible for persisting these changes and for updating the
    /// associated <see cref="WorkOrder"/> status once all tokens are complete.
    ///
    /// In a multi-token (parallel gateway) workflow, the instance status should only
    /// be set to Completed after ALL tokens finish.  For now the minimal single-token
    /// implementation sets it here; the runtime checks the token status after execution
    /// to decide whether to finalise the work order.
    /// </summary>
    public class EndNodeExecutor : IWorkflowNodeExecutor
    {
        /// <inheritdoc/>
        public WorkflowNodeType NodeType => WorkflowNodeType.End;

        /// <inheritdoc/>
        public Task<NodeExecutionResult> ExecuteAsync(
            NodeExecutionContext context,
            CancellationToken cancellationToken)
        {
            // Complete the node instance record
            context.NodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
            context.NodeInstance.CompletedAt = DateTime.UtcNow;

            // Complete the execution token — this thread of control is finished
            context.Token.Status = WorkflowExecutionTokenStatus.Completed;
            context.Token.UpdatedAt = DateTime.UtcNow;

            // Mark the workflow instance as fully completed
            context.Instance.Status = WorkflowInstanceStatus.Completed;
            context.Instance.CompletedAt = DateTime.UtcNow;

            // Return Done: runtime will detect the Completed token and update the WorkOrder
            return Task.FromResult(NodeExecutionResult.Done());
        }
    }
}
