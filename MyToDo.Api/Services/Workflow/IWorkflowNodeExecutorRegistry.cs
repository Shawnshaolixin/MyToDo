using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public interface IWorkflowNodeExecutorRegistry
    {
        void Register(IWorkflowNodeExecutor executor);

        IWorkflowNodeExecutor GetExecutor(WorkflowNodeType nodeType);
    }
}
