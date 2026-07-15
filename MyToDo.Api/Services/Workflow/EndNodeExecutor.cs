using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Handles the End node: returns Done to signal the runtime that the
    /// workflow instance has reached its terminal node and should be
    /// marked as Completed.
    /// </summary>
    public class EndNodeExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.End;

        public Task<NodeExecutionResult> ExecuteAsync(
            WorkflowNodeExecutionContext context,
            CancellationToken cancellationToken)
        {
            // The End node has no side-effects; returning Done lets the runtime
            // detect there are no outgoing edges and close the instance.
            return Task.FromResult(NodeExecutionResult.Done());
        }
    }
}
