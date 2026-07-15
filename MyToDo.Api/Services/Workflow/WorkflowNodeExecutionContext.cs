using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class WorkflowNodeExecutionContext
    {
        public WorkflowInstance WorkflowInstance { get; init; } = null!;
        public WorkflowExecutionToken ExecutionToken { get; init; } = null!;
        public WorkflowNode WorkflowNode { get; init; } = null!;
        public WorkflowNodeInstance WorkflowNodeInstance { get; init; } = null!;
        public WorkOrder WorkOrder { get; init; } = null!;
        public object? Input { get; init; }
    }
}
