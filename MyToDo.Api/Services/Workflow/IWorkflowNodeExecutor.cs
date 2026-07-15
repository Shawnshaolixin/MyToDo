using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public interface IWorkflowNodeExecutor
    {
        WorkflowNodeType NodeType { get; }

        Task<NodeExecutionResult> ExecuteAsync(WorkflowNodeExecutionContext context, CancellationToken cancellationToken);
    }
}
