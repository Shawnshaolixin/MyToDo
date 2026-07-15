using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow.Executors
{
    /// <summary>
    /// Handles the End node: marks the workflow instance and work order as completed.
    /// </summary>
    public class EndNodeExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.End;

        public async Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
        {
            context.NodeInstance.Status = WorkflowNodeInstanceStatus.Completed;
            context.NodeInstance.CompletedAt = DateTime.UtcNow;

            context.Token.Status = WorkflowExecutionTokenStatus.Completed;
            context.Token.UpdatedAt = DateTime.UtcNow;

            context.Instance.Status = WorkflowInstanceStatus.Completed;
            context.Instance.CompletedAt = DateTime.UtcNow;

            var workOrder = await context.DbContext.WorkOrders
                .FirstAsync(x => x.Id == context.Instance.WorkOrderId, cancellationToken);
            workOrder.Status = WorkOrderStatus.Completed;
            workOrder.CompletedAt = DateTime.UtcNow;

            return NodeExecutionResult.Terminate();
        }
    }
}
