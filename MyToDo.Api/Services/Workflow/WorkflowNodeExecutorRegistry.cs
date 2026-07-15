using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Registry for mapping workflow node type -> executor implementation.
    /// Duplicate registrations are rejected to avoid ambiguous runtime behavior.
    /// </summary>
    public class WorkflowNodeExecutorRegistry : IWorkflowNodeExecutorRegistry
    {
        private readonly IReadOnlyDictionary<WorkflowNodeType, IWorkflowNodeExecutor> _executors;

        public WorkflowNodeExecutorRegistry(IEnumerable<IWorkflowNodeExecutor> executors)
        {
            var map = new Dictionary<WorkflowNodeType, IWorkflowNodeExecutor>();
            foreach (var executor in executors)
            {
                if (!map.TryAdd(executor.NodeType, executor))
                {
                    throw new InvalidOperationException($"Duplicate node executor registration for {executor.NodeType}.");
                }
            }

            _executors = map;
        }

        public IWorkflowNodeExecutor Find(WorkflowNodeType nodeType)
        {
            if (_executors.TryGetValue(nodeType, out var executor))
            {
                return executor;
            }

            throw new InvalidOperationException($"No node executor registered for {nodeType}.");
        }
    }
}
