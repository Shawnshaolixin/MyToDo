using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Start node does not block and always completes immediately.
    /// </summary>
    public class StartNodeExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.Start;

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext context)
        {
            return Task.FromResult(NodeExecutionResult.Done());
        }
    }
}
