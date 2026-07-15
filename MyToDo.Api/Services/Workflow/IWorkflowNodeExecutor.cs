using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public interface IWorkflowNodeExecutor
    {
        WorkflowNodeType NodeType { get; }
        Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext context);
    }
}
