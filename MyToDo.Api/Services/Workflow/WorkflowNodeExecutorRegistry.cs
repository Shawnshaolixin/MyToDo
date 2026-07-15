using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Resolves the correct <see cref="IWorkflowNodeExecutor"/> for a given <see cref="WorkflowNodeType"/>.
    /// All registered <see cref="IWorkflowNodeExecutor"/> implementations are injected via DI.
    /// </summary>
    public class WorkflowNodeExecutorRegistry
    {
        private readonly Dictionary<WorkflowNodeType, IWorkflowNodeExecutor> _executors;

        public WorkflowNodeExecutorRegistry(IEnumerable<IWorkflowNodeExecutor> executors)
        {
            _executors = executors.ToDictionary(e => e.NodeType);
        }

        public IWorkflowNodeExecutor GetExecutor(WorkflowNodeType nodeType)
        {
            if (_executors.TryGetValue(nodeType, out var executor))
                return executor;
            throw new InvalidOperationException($"No executor registered for node type: {nodeType}");
        }
    }
}
