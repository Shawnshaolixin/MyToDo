using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Carries the per-iteration state that node executors may read or mutate.
    /// The DbContext is included so executors can create related entities (e.g. SchedulableTask).
    /// </summary>
    public class NodeExecutionContext
    {
        public required WorkflowInstance Instance { get; init; }
        public required WorkflowExecutionToken Token { get; init; }
        public required WorkflowNode Node { get; init; }
        public required WorkflowNodeInstance NodeInstance { get; init; }
        public required MyToDoContext DbContext { get; init; }
    }

    public enum NodeExecutionOutcome
    {
        /// <summary>Node completed normally; the runtime should advance the token to the next node.</summary>
        Completed,
        /// <summary>Node is waiting for an external event (bookmark created); the runtime should stop.</summary>
        Suspended,
        /// <summary>Workflow has reached its terminal state (End node); the runtime should stop.</summary>
        Terminated
    }

    public class NodeExecutionResult
    {
        public NodeExecutionOutcome Outcome { get; private init; }

        public static NodeExecutionResult Continue() => new() { Outcome = NodeExecutionOutcome.Completed };
        public static NodeExecutionResult Suspend() => new() { Outcome = NodeExecutionOutcome.Suspended };
        public static NodeExecutionResult Terminate() => new() { Outcome = NodeExecutionOutcome.Terminated };
    }

    /// <summary>
    /// Executes the business logic for a single workflow node type.
    /// Implementations must not call SaveChanges; the runtime handles persistence.
    /// </summary>
    public interface IWorkflowNodeExecutor
    {
        WorkflowNodeType NodeType { get; }
        Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken);
    }
}
