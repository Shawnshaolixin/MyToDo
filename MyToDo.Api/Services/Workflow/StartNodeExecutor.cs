using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class StartNodeExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.Start;

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowNodeExecutionContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(NodeExecutionResult.Done());
        }
    }
}
