using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Entities.Workflow;
using MyToDo.Api.Services.Workstation;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Executes a WorkstationTask node by:
    ///   1. Locating the associated Workstation from the node's RequiredResourceType / WorkstationId.
    ///   2. Creating a <see cref="WorkstationTaskInstance"/> in PendingConfig stage.
    ///   3. Returning Waiting with a bookmark so the operator can configure and
    ///      start the experiment via the workstation tasks API.
    ///
    /// The workflow resumes when WorkstationEventAppService receives an
    /// ExperimentCompleted or ExperimentFailed event and calls
    /// IWorkflowRuntime.ResumeAsync with <see cref="WorkflowBookmarkTypes.WorkstationTaskCompleted"/>.
    /// </summary>
    public class WorkstationTaskExecutor : IWorkflowNodeExecutor
    {
        private readonly IWorkstationGateway _gateway;

        public WorkstationTaskExecutor(IWorkstationGateway gateway)
        {
            _gateway = gateway;
        }

        public WorkflowNodeType NodeType => WorkflowNodeType.WorkstationTask;

        public async Task<NodeExecutionResult> ExecuteAsync(
            WorkflowNodeExecutionContext context,
            CancellationToken cancellationToken)
        {
            // Locate the workstation — find by code stored in node RequiredResourceType,
            // or fall back to the first active workstation.
            var workstation = await context.DbContext.Workstations
                .FirstOrDefaultAsync(
                    w => w.IsActive && (context.Node.RequiredResourceType == null
                         || w.Code == context.Node.RequiredResourceType
                         || w.Name == context.Node.RequiredResourceType),
                    cancellationToken)
                ?? await context.DbContext.Workstations
                    .FirstOrDefaultAsync(w => w.IsActive, cancellationToken);

            if (workstation == null)
            {
                return NodeExecutionResult.Failed("No active workstation found for WorkstationTask node.");
            }

            var taskInstance = new WorkstationTaskInstance
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = context.Instance.Id,
                WorkflowNodeInstanceId = context.NodeInstance.Id,
                WorkstationId = workstation.Id,
                Stage = WorkstationTaskStage.PendingConfig,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.DbContext.WorkstationTaskInstances.Add(taskInstance);
            // Changes are saved by the caller (WorkflowRuntime) after this returns.

            // Bookmark key: "<instanceId>:<nodeId>" — consumed by WorkstationEventAppService.
            var bookmarkKey = $"{context.Instance.Id}:{context.Node.Id}";
            return NodeExecutionResult.Waiting(WorkflowBookmarkTypes.WorkstationTaskCompleted, bookmarkKey);
        }
    }
}
