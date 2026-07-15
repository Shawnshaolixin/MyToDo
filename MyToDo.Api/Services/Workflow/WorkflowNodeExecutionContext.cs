using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// 封装节点执行时所需的流程上下文，避免执行器自行重复查询流程主数据。
    /// </summary>
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
