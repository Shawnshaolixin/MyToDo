using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Dictionary-backed implementation of <see cref="IWorkflowNodeExecutorRegistry"/>.
    ///
    /// Registered as a singleton so the lookup dictionary is built once at startup
    /// and never mutated during normal request processing (thread-safe for concurrent reads).
    /// </summary>
    public class WorkflowNodeExecutorRegistry : IWorkflowNodeExecutorRegistry
    {
        // NodeType → executor; populated during application startup before any requests arrive
        private readonly Dictionary<WorkflowNodeType, IWorkflowNodeExecutor> _executors = new();

        /// <inheritdoc/>
        public void Register(IWorkflowNodeExecutor executor)
        {
            // Last registration for a given node type wins — allows tests to override defaults
            _executors[executor.NodeType] = executor;
        }

        /// <inheritdoc/>
        public IWorkflowNodeExecutor? Resolve(WorkflowNodeType nodeType)
        {
            // Returns null rather than throwing so the caller can provide a descriptive error
            _executors.TryGetValue(nodeType, out var executor);
            return executor;
        }
    }
}
