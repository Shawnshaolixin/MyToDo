using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;
using MyToDo.Api.Services.Workflow;

namespace MyToDo.Api.Services.Workstation
{
    /// <summary>Input model for receiving a device event.</summary>
    public record WorkstationEventInput(
        string WorkstationCode,
        string DeviceJobId,
        string EventType,      // "ExperimentCompleted" | "ExperimentFailed" | "PromptRaised" | ...
        string? PayloadJson);

    /// <summary>
    /// Processes inbound events from workstation devices:
    ///   - Persists the event as a <see cref="WorkstationEvent"/>.
    ///   - On ExperimentCompleted/Failed: updates WorkstationTaskInstance stage and
    ///     calls IWorkflowRuntime.ResumeAsync to unblock the suspended workflow.
    ///   - On PromptRaised: creates a <see cref="WorkstationPrompt"/> for operator action.
    /// </summary>
    public class WorkstationEventAppService
    {
        private readonly MyToDoContext _context;
        private readonly IWorkflowRuntime _workflowRuntime;

        public WorkstationEventAppService(MyToDoContext context, IWorkflowRuntime workflowRuntime)
        {
            _context = context;
            _workflowRuntime = workflowRuntime;
        }

        /// <summary>Handles a single event received from a device.</summary>
        public async Task HandleEventAsync(WorkstationEventInput input, CancellationToken cancellationToken)
        {
            // 1. Resolve workstation
            var workstation = await _context.Workstations
                .FirstOrDefaultAsync(w => w.Code == input.WorkstationCode, cancellationToken)
                ?? throw new InvalidOperationException($"Unknown workstation code: {input.WorkstationCode}");

            // 2. Persist the raw event
            if (!Enum.TryParse<WorkstationEventType>(input.EventType, ignoreCase: true, out var eventType))
            {
                eventType = WorkstationEventType.StatusUpdate;
            }

            var wsEvent = new WorkstationEvent
            {
                Id = Guid.NewGuid(),
                WorkstationId = workstation.Id,
                DeviceJobId = input.DeviceJobId,
                EventType = eventType,
                PayloadJson = input.PayloadJson,
                ReceivedAt = DateTime.UtcNow
            };

            _context.WorkstationEvents.Add(wsEvent);
            await _context.SaveChangesAsync(cancellationToken);

            // 3. Handle event-specific logic
            switch (eventType)
            {
                case WorkstationEventType.ExperimentCompleted:
                    await HandleCompletionAsync(input, completed: true, cancellationToken);
                    break;

                case WorkstationEventType.ExperimentFailed:
                    await HandleCompletionAsync(input, completed: false, cancellationToken);
                    break;

                case WorkstationEventType.PromptRaised:
                    await HandlePromptAsync(input, cancellationToken);
                    break;

                default:
                    // StatusUpdate and others — no further action needed
                    break;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task HandleCompletionAsync(
            WorkstationEventInput input,
            bool completed,
            CancellationToken cancellationToken)
        {
            var taskInstance = await _context.WorkstationTaskInstances
                .FirstOrDefaultAsync(t => t.DeviceJobId == input.DeviceJobId, cancellationToken);

            if (taskInstance == null)
            {
                return; // Unknown job — log and ignore in production
            }

            taskInstance.Stage = completed ? WorkstationTaskStage.Completed : WorkstationTaskStage.Failed;
            taskInstance.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Locate the WorkflowNode for this task instance to reconstruct the bookmark key
            var nodeInstance = await _context.WorkflowNodeInstances
                .FirstOrDefaultAsync(n => n.Id == taskInstance.WorkflowNodeInstanceId, cancellationToken);

            if (nodeInstance == null) return;

            var bookmarkKey = $"{taskInstance.WorkflowInstanceId}:{nodeInstance.WorkflowNodeId}";

            // Resume the suspended workflow token
            await _workflowRuntime.ResumeAsync(
                WorkflowBookmarkTypes.WorkstationTaskCompleted,
                bookmarkKey,
                null,
                cancellationToken);
        }

        private async Task HandlePromptAsync(
            WorkstationEventInput input,
            CancellationToken cancellationToken)
        {
            var taskInstance = await _context.WorkstationTaskInstances
                .FirstOrDefaultAsync(t => t.DeviceJobId == input.DeviceJobId, cancellationToken);

            if (taskInstance == null) return;

            // Parse promptCode from payload — default to "Unknown"
            var promptCode = "Unknown";
            if (!string.IsNullOrWhiteSpace(input.PayloadJson))
            {
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(input.PayloadJson);
                    if (doc.RootElement.TryGetProperty("promptCode", out var pc))
                    {
                        promptCode = pc.GetString() ?? promptCode;
                    }
                }
                catch { /* ignore parse errors */ }
            }

            var prompt = new WorkstationPrompt
            {
                Id = Guid.NewGuid(),
                WorkstationTaskInstanceId = taskInstance.Id,
                PromptCode = promptCode,
                Message = input.PayloadJson,
                Status = WorkstationPromptStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkstationPrompts.Add(prompt);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
