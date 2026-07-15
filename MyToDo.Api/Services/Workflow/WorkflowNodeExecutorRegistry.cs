using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Provides registration and lookup of <see cref="IWorkflowNodeExecutor"/>
    /// implementations by <see cref="WorkflowNodeType"/>.
    ///
    /// Executors are registered at startup (via DI or explicit call to
    /// <see cref="Register"/>) and resolved by the runtime for each node
    /// it processes.  This pattern decouples the runtime engine from
    /// node-specific logic and makes it easy to add custom node types.
    /// </summary>
    public interface IWorkflowNodeExecutorRegistry
    {
        /// <summary>Registers an executor. Later registrations overwrite earlier ones for the same type.</summary>
        void Register(IWorkflowNodeExecutor executor);

        /// <summary>Returns the executor for <paramref name="nodeType"/>, or <c>null</c> if none is registered.</summary>
        IWorkflowNodeExecutor? GetExecutor(WorkflowNodeType nodeType);
    }

    /// <summary>Default in-memory implementation of <see cref="IWorkflowNodeExecutorRegistry"/>.</summary>
    public class WorkflowNodeExecutorRegistry : IWorkflowNodeExecutorRegistry
    {
        private readonly Dictionary<WorkflowNodeType, IWorkflowNodeExecutor> _executors = new();

        /// <inheritdoc/>
        public void Register(IWorkflowNodeExecutor executor)
        {
            _executors[executor.NodeType] = executor;
        }

        /// <inheritdoc/>
        public IWorkflowNodeExecutor? GetExecutor(WorkflowNodeType nodeType)
        {
            _executors.TryGetValue(nodeType, out var executor);
            return executor;
        }
    }
}
