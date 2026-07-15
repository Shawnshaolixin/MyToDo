namespace MyToDo.Api.Entities.Workflow
{
    /// <summary>Lifecycle stages of a workstation task instance.</summary>
    public enum WorkstationTaskStage
    {
        /// <summary>Awaiting experiment configuration from the user.</summary>
        PendingConfig = 0,

        /// <summary>Experiment configured; ready to be sent to device.</summary>
        Configured = 1,

        /// <summary>Experiment sent to device and running.</summary>
        Running = 2,

        /// <summary>Device reported completion.</summary>
        Completed = 3,

        /// <summary>Device reported failure.</summary>
        Failed = 4
    }

    /// <summary>Type of event received from a workstation device.</summary>
    public enum WorkstationEventType
    {
        ExperimentStarted = 0,
        ExperimentCompleted = 1,
        ExperimentFailed = 2,
        PromptRaised = 3,
        StatusUpdate = 4
    }

    /// <summary>Status of an operator prompt that requires resolution.</summary>
    public enum WorkstationPromptStatus
    {
        Pending = 0,
        Resolved = 1
    }

    public static class WorkflowBookmarkTypesExtensions
    {
        // Bookmark type re-export so callers only need one using.
        public const string WorkstationTaskCompleted = WorkflowBookmarkTypes.WorkstationTaskCompleted;
    }
}
