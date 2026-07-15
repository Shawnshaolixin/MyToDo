using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>Outcome of a single node execution step.</summary>
    public enum NodeExecutionOutcome
    {
        /// <summary>Node completed; the runtime should advance to the next node.</summary>
        Done = 0,

        /// <summary>Node is waiting for an external event; the runtime creates a bookmark and suspends the token.</summary>
        Waiting = 1,

        /// <summary>Node execution failed; the runtime marks the node and instance as failed.</summary>
        Failed = 2
    }

    /// <summary>Result returned by a node executor.</summary>
    public class NodeExecutionResult
    {
        public NodeExecutionOutcome Outcome { get; init; }

        /// <summary>Human-readable error message (only relevant when <see cref="Outcome"/> is <see cref="NodeExecutionOutcome.Failed"/>).</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>Bookmark type to create when the outcome is <see cref="NodeExecutionOutcome.Waiting"/>.</summary>
        public string? BookmarkType { get; init; }

        /// <summary>Bookmark key to create when the outcome is <see cref="NodeExecutionOutcome.Waiting"/>.</summary>
        public string? BookmarkKey { get; init; }

        public static NodeExecutionResult Done() => new() { Outcome = NodeExecutionOutcome.Done };
        public static NodeExecutionResult Waiting(string bookmarkType, string bookmarkKey) =>
            new() { Outcome = NodeExecutionOutcome.Waiting, BookmarkType = bookmarkType, BookmarkKey = bookmarkKey };
        public static NodeExecutionResult Failed(string error) =>
            new() { Outcome = NodeExecutionOutcome.Failed, ErrorMessage = error };
    }

    /// <summary>Context passed to a node executor during execution.</summary>
    public class WorkflowNodeExecutionContext
    {
        public required WorkflowInstance Instance { get; init; }
        public required WorkflowExecutionToken Token { get; init; }
        public required WorkflowNodeInstance NodeInstance { get; init; }
        public required WorkflowNode Node { get; init; }
        public required MyToDoContext DbContext { get; init; }
    }

    /// <summary>
    /// Executes a single workflow node type.
    /// Implementations are registered with <see cref="IWorkflowNodeExecutorRegistry"/>
    /// and resolved by <see cref="WorkflowNodeType"/>.
    /// </summary>
    public interface IWorkflowNodeExecutor
    {
        /// <summary>The node type this executor handles.</summary>
        WorkflowNodeType NodeType { get; }

        /// <summary>Executes the node and returns the outcome.</summary>
        Task<NodeExecutionResult> ExecuteAsync(WorkflowNodeExecutionContext context, CancellationToken cancellationToken);
    }
}
