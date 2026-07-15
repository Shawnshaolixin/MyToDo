using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public interface IWorkflowNodeExecutorRegistry
    {
        IWorkflowNodeExecutor Find(WorkflowNodeType nodeType);
    }
}
