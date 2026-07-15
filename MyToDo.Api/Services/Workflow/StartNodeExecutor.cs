using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Handles the Start node: immediately returns Done so the runtime
    /// advances the token to the first downstream node.
    /// </summary>
    public class StartNodeExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.Start;

        public Task<NodeExecutionResult> ExecuteAsync(
            WorkflowNodeExecutionContext context,
            CancellationToken cancellationToken)
        {
            // The Start node has no side-effects; just mark it complete.
            return Task.FromResult(NodeExecutionResult.Done());
        }
    }
}
