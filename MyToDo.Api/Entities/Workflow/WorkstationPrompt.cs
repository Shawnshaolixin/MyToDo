namespace MyToDo.Api.Entities.Workflow
{
    /// <summary>
    /// An operator prompt raised during a workstation experiment that requires
    /// human resolution before the experiment can continue.
    /// </summary>
    public class WorkstationPrompt
    {
        public Guid Id { get; set; }

        public Guid WorkstationTaskInstanceId { get; set; }

        /// <summary>Short code identifying the type of prompt (e.g. "ConfirmSample").</summary>
        public string PromptCode { get; set; } = string.Empty;

        public string? Message { get; set; }

        /// <summary>JSON payload describing the options available to the operator.</summary>
        public string? OptionsJson { get; set; }

        /// <summary>The operator's resolution choice.</summary>
        public string? Resolution { get; set; }

        public WorkstationPromptStatus Status { get; set; } = WorkstationPromptStatus.Pending;

        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public WorkstationTaskInstance? WorkstationTaskInstance { get; set; }
    }
}
