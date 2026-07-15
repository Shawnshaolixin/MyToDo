namespace MyToDo.Api.Entities.Workflow
{
    /// <summary>
    /// Tracks the runtime state of a WorkstationTask node execution:
    /// the selected experiment, parameters sent to the device, and current stage.
    /// </summary>
    public class WorkstationTaskInstance
    {
        public Guid Id { get; set; }

        public Guid WorkflowInstanceId { get; set; }

        /// <summary>The WorkflowNodeInstance that created this task instance.</summary>
        public Guid WorkflowNodeInstanceId { get; set; }

        public Guid WorkstationId { get; set; }

        /// <summary>Identifier of the experiment definition selected for this run.</summary>
        public string? ExperimentDefinitionId { get; set; }

        /// <summary>JSON blob of parameters supplied by the operator.</summary>
        public string? ParametersJson { get; set; }

        /// <summary>Job identifier returned by the device gateway on StartExperiment.</summary>
        public string? DeviceJobId { get; set; }

        public WorkstationTaskStage Stage { get; set; } = WorkstationTaskStage.PendingConfig;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Workstation? Workstation { get; set; }
        public WorkflowNodeInstance? WorkflowNodeInstance { get; set; }
    }
}
