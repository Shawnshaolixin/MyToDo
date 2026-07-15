using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Mutable execution context for a single node run.
    /// Executors can update instance/token/work-order status as needed.
    /// </summary>
    public class WorkflowExecutionContext
    {
        public required MyToDoContext DbContext { get; init; }
        public required WorkflowInstance Instance { get; init; }
        public required WorkflowExecutionToken Token { get; init; }
        public required WorkflowNode Node { get; init; }
        public required WorkflowNodeInstance NodeInstance { get; init; }
        public required WorkOrder WorkOrder { get; init; }
        public CancellationToken CancellationToken { get; init; }
    }
}
