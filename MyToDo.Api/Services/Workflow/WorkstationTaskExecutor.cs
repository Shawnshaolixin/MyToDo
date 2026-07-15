using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// WorkstationTask pauses execution and waits for external workstation completion signal.
    /// The bookmark key uses "workflowInstanceId:nodeId" so external callbacks can resume deterministically.
    /// </summary>
    public class WorkstationTaskExecutor : IWorkflowNodeExecutor
    {
        public WorkflowNodeType NodeType => WorkflowNodeType.WorkstationTask;

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext context)
        {
            var bookmarkKey = $"{context.Instance.Id}:{context.Node.Id}";
            return Task.FromResult(NodeExecutionResult.Waiting(WorkflowBookmarkTypes.WorkstationTaskCompleted, bookmarkKey));
        }
    }
}
