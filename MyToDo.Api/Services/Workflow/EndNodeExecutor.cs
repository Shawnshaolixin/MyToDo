using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class EndNodeExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.End;

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowNodeExecutionContext context, CancellationToken cancellationToken)
        {
            context.ExecutionToken.Status = WorkflowExecutionTokenStatus.Completed;
            context.ExecutionToken.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(NodeExecutionResult.Done());
        }
    }
}
