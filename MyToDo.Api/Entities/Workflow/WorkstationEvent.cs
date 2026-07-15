namespace MyToDo.Api.Entities.Workflow
{
    /// <summary>
    /// An event received from a workstation device (e.g. ExperimentCompleted, PromptRaised).
    /// Events are persisted for audit and to drive workflow resumption.
    /// </summary>
    public class WorkstationEvent
    {
        public Guid Id { get; set; }

        public Guid WorkstationId { get; set; }

        /// <summary>Device-issued job identifier echoed back in every event.</summary>
        public string DeviceJobId { get; set; } = string.Empty;

        public WorkstationEventType EventType { get; set; }

        /// <summary>Raw JSON payload from the device.</summary>
        public string? PayloadJson { get; set; }

        public DateTime ReceivedAt { get; set; }

        public Workstation? Workstation { get; set; }
    }
}
