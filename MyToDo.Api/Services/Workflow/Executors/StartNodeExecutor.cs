using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow.Executors
{
    /// <summary>
    /// Handles the Start node: marks the node instance complete and signals the runtime to advance.
    /// </summary>
    public class StartNodeExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.Start;

        public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
        {
            context.NodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
            context.NodeInstance.CompletedAt = DateTime.UtcNow;
            return Task.FromResult(NodeExecutionResult.Continue());
        }
    }
}
