using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workstation
{
    /// <summary>
    /// Application service for managing the lifecycle of a WorkstationTaskInstance:
    /// browsing available experiments, saving configuration, and starting the experiment
    /// on the device via <see cref="IWorkstationGateway"/>.
    /// </summary>
    public class WorkstationTaskAppService
    {
        private readonly MyToDoContext _context;
        private readonly IWorkstationGateway _gateway;

        public WorkstationTaskAppService(MyToDoContext context, IWorkstationGateway gateway)
        {
            _context = context;
            _gateway = gateway;
        }

        /// <summary>
        /// Returns the experiments available on the workstation associated with
        /// the given WorkflowNodeInstance.
        /// </summary>
        public async Task<IReadOnlyList<ExperimentDto>> GetExperimentsAsync(
            Guid workflowNodeInstanceId,
            CancellationToken cancellationToken)
        {
            var taskInstance = await GetTaskInstanceAsync(workflowNodeInstanceId, cancellationToken);
            var workstation = await _context.Workstations
                .FirstAsync(w => w.Id == taskInstance.WorkstationId, cancellationToken);

            return await _gateway.GetExperimentsAsync(workstation.Code, cancellationToken);
        }

        /// <summary>Returns the parameter schema for the chosen experiment.</summary>
        public async Task<IReadOnlyList<ExperimentParameterSchemaDto>> GetExperimentParametersAsync(
            Guid workflowNodeInstanceId,
            string experimentId,
            CancellationToken cancellationToken)
        {
            var taskInstance = await GetTaskInstanceAsync(workflowNodeInstanceId, cancellationToken);
            var workstation = await _context.Workstations
                .FirstAsync(w => w.Id == taskInstance.WorkstationId, cancellationToken);

            return await _gateway.GetExperimentParametersAsync(workstation.Code, experimentId, cancellationToken);
        }

        /// <summary>Saves the experiment selection and parameters (Stage → Configured).</summary>
        public async Task SaveConfigAsync(
            Guid workflowNodeInstanceId,
            string experimentDefinitionId,
            string parametersJson,
            CancellationToken cancellationToken)
        {
            var taskInstance = await GetTaskInstanceAsync(workflowNodeInstanceId, cancellationToken);

            taskInstance.ExperimentDefinitionId = experimentDefinitionId;
            taskInstance.ParametersJson = parametersJson;
            taskInstance.Stage = WorkstationTaskStage.Configured;
            taskInstance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Sends the start command to the device (Stage → Running) and stores the DeviceJobId.
        /// </summary>
        public async Task<StartExperimentResult> StartExperimentAsync(
            Guid workflowNodeInstanceId,
            CancellationToken cancellationToken)
        {
            var taskInstance = await GetTaskInstanceAsync(workflowNodeInstanceId, cancellationToken);

            if (taskInstance.Stage != WorkstationTaskStage.Configured)
            {
                return new StartExperimentResult(false, null, "Task instance must be in Configured stage before starting.");
            }

            var workstation = await _context.Workstations
                .FirstAsync(w => w.Id == taskInstance.WorkstationId, cancellationToken);

            var result = await _gateway.StartExperimentAsync(
                workstation.Code,
                taskInstance.ExperimentDefinitionId ?? string.Empty,
                taskInstance.ParametersJson ?? "{}",
                cancellationToken);

            if (result.Success)
            {
                taskInstance.DeviceJobId = result.DeviceJobId;
                taskInstance.Stage = WorkstationTaskStage.Running;
                taskInstance.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Sends an operator prompt resolution to the device via the gateway.
        /// </summary>
        public async Task<CommandResult> ResolvePromptAsync(
            Guid promptId,
            string resolution,
            CancellationToken cancellationToken)
        {
            var prompt = await _context.WorkstationPrompts
                .Include(p => p.WorkstationTaskInstance)
                    .ThenInclude(t => t!.Workstation)
                .FirstAsync(p => p.Id == promptId, cancellationToken);

            var taskInstance = prompt.WorkstationTaskInstance!;
            var workstation = taskInstance.Workstation!;

            var result = await _gateway.ApplyResolutionAsync(
                workstation.Code,
                taskInstance.DeviceJobId ?? string.Empty,
                prompt.PromptCode,
                resolution,
                cancellationToken);

            if (result.Success)
            {
                prompt.Resolution = resolution;
                prompt.Status = WorkstationPromptStatus.Resolved;
                prompt.ResolvedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task<WorkstationTaskInstance> GetTaskInstanceAsync(
            Guid workflowNodeInstanceId,
            CancellationToken cancellationToken)
        {
            return await _context.WorkstationTaskInstances
                .FirstOrDefaultAsync(t => t.WorkflowNodeInstanceId == workflowNodeInstanceId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"WorkstationTaskInstance not found for WorkflowNodeInstanceId {workflowNodeInstanceId}.");
        }
    }
}
