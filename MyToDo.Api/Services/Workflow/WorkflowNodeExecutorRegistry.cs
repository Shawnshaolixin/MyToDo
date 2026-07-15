using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class WorkflowNodeExecutorRegistry : IWorkflowNodeExecutorRegistry
    {
        private readonly Dictionary<WorkflowNodeType, IWorkflowNodeExecutor> _executors = new();

        public WorkflowNodeExecutorRegistry(IEnumerable<IWorkflowNodeExecutor> executors)
        {
            foreach (var executor in executors)
            {
                Register(executor);
            }
        }

        public void Register(IWorkflowNodeExecutor executor)
        {
            _executors[executor.NodeType] = executor;
        }

        public IWorkflowNodeExecutor GetExecutor(WorkflowNodeType nodeType)
        {
            if (_executors.TryGetValue(nodeType, out var executor))
            {
                return executor;
            }

            throw new InvalidOperationException($"No executor registered for node type: {nodeType}");
        }
    }
}
