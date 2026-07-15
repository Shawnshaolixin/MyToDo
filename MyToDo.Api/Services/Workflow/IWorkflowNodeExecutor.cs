using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Outcome returned by an <see cref="IWorkflowNodeExecutor"/> after executing a node.
    /// </summary>
    public enum NodeExecutionOutcome
    {
        /// <summary>
        /// The node finished its work synchronously.
        /// <see cref="IWorkflowRuntime"/> will advance the execution token to the
        /// next downstream node and continue the execution loop.
        /// </summary>
        Done,

        /// <summary>
        /// The node is suspended waiting for an external event.
        /// The executor has already staged a <see cref="WorkflowBookmark"/> via
        /// <see cref="IWorkflowBookmarkService.CreateAsync"/> which persisted all
        /// pending changes atomically.
        /// <see cref="IWorkflowRuntime"/> will stop the execution loop; the workflow
        /// resumes when <see cref="IWorkflowRuntime.ResumeAsync"/> is called with the
        /// matching bookmark type and key.
        /// </summary>
        Waiting
    }

    /// <summary>
    /// Result returned by an <see cref="IWorkflowNodeExecutor"/>.
    /// Use the static factory methods <see cref="Done"/> and <see cref="Waiting"/> for clarity.
    /// </summary>
    public class NodeExecutionResult
    {
        /// <summary>Whether the node completed or is waiting for an external event.</summary>
        public NodeExecutionOutcome Outcome { get; private init; }

        /// <summary>
        /// Factory: the node finished synchronously and the runtime should advance the token.
        /// </summary>
        public static NodeExecutionResult Done() => new() { Outcome = NodeExecutionOutcome.Done };

        /// <summary>
        /// Factory: the node suspended itself (bookmark already persisted by the executor).
        /// </summary>
        public static NodeExecutionResult Waiting() => new() { Outcome = NodeExecutionOutcome.Waiting };
    }

    /// <summary>
    /// All live entities available to a node executor during execution.
    /// Executors may mutate these objects; the calling runtime persists changes.
    /// </summary>
    public class NodeExecutionContext
    {
        /// <summary>The running workflow instance.</summary>
        public required WorkflowInstance Instance { get; init; }

        /// <summary>The execution token currently pointing at the node being executed.</summary>
        public required WorkflowExecutionToken Token { get; init; }

        /// <summary>The static workflow node definition (type, resource requirements, etc.).</summary>
        public required WorkflowNode Node { get; init; }

        /// <summary>
        /// The runtime record for this specific execution of the node.
        /// Newly created and already added to the DbContext change tracker before
        /// the executor is called.
        /// </summary>
        public required WorkflowNodeInstance NodeInstance { get; init; }
    }

    /// <summary>
    /// Handles execution logic for a single <see cref="WorkflowNodeType"/>.
    /// Implementations are registered with <see cref="IWorkflowNodeExecutorRegistry"/>
    /// at application startup and resolved by <see cref="WorkflowRuntime"/> at runtime.
    ///
    /// An executor may:
    /// <list type="bullet">
    ///   <item>Mutate entities in <see cref="NodeExecutionContext"/> (e.g. set status).</item>
    ///   <item>Stage new entities via an injected DbContext (without calling SaveChangesAsync).</item>
    ///   <item>Call <see cref="IWorkflowBookmarkService.CreateAsync"/> which flushes all pending
    ///         changes atomically when the node must suspend.</item>
    /// </list>
    /// </summary>
    public interface IWorkflowNodeExecutor
    {
        /// <summary>The node type this executor handles.</summary>
        WorkflowNodeType NodeType { get; }

        /// <summary>
        /// Execute the node described by <paramref name="context"/>.
        /// Returns <see cref="NodeExecutionOutcome.Done"/> when the node finished, or
        /// <see cref="NodeExecutionOutcome.Waiting"/> when a bookmark was created and
        /// the workflow must pause until resumed.
        /// </summary>
        Task<NodeExecutionResult> ExecuteAsync(
            NodeExecutionContext context,
            CancellationToken cancellationToken);
    }
}
