using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// End node marks the execution token and instance as completed.
    /// </summary>
    public class EndNodeExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.End;

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext context)
        {
            context.Token.Status = WorkflowExecutionTokenStatus.Completed;
            context.Token.UpdatedAt = DateTime.UtcNow;

            context.Instance.Status = WorkflowInstanceStatus.Completed;
            context.Instance.CompletedAt = DateTime.UtcNow;

            context.WorkOrder.Status = WorkOrderStatus.Completed;
            context.WorkOrder.CompletedAt = DateTime.UtcNow;

            return Task.FromResult(NodeExecutionResult.Done());
        }
    }
}
