using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Maps <see cref="WorkflowNodeType"/> values to their corresponding
    /// <see cref="IWorkflowNodeExecutor"/> implementations.
    ///
    /// Executors are registered once at application startup (via
    /// <see cref="Register"/>) and then resolved on every node execution by
    /// <see cref="WorkflowRuntime"/>.  The registry itself is registered as a
    /// singleton so the dictionary is built once and shared across requests.
    /// </summary>
    public interface IWorkflowNodeExecutorRegistry
    {
        /// <summary>
        /// Register <paramref name="executor"/> for its declared
        /// <see cref="IWorkflowNodeExecutor.NodeType"/>.
        /// Overwrites any previously registered executor for the same type —
        /// useful for overriding the default implementation in tests.
        /// </summary>
        void Register(IWorkflowNodeExecutor executor);

        /// <summary>
        /// Returns the executor registered for <paramref name="nodeType"/>,
        /// or <c>null</c> if none has been registered.
        /// </summary>
        IWorkflowNodeExecutor? Resolve(WorkflowNodeType nodeType);
    }
}
