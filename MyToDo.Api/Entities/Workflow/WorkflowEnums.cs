namespace MyToDo.Api.Entities.Workflow
{
    public enum WorkflowNodeType
    {
        Start = 0,
        End = 1,
        ScheduleTask = 2,
        WorkstationTask = 3
    }

    public enum WorkflowInstanceStatus
    {
        Running = 0,
        Suspended = 1,
        Completed = 2,
        Failed = 3
    }

    public enum WorkflowExecutionTokenStatus
    {
        Ready = 0,
        Waiting = 1,
        Completed = 2,
        Failed = 3
    }

    public enum WorkflowNodeInstanceStatus
    {
        Running = 0,
        Waiting = 1,
        Completed = 2,
        Failed = 3
    }

    public enum WorkflowBookmarkStatus
    {
        Active = 0,
        Consumed = 1
    }

    public enum WorkOrderStatus
    {
        Draft = 0,
        Submitted = 1,
        InProgress = 2,
        Completed = 3,
        Failed = 4
    }

    public enum SchedulableTaskStatus
    {
        ReadyForScheduling = 0,
        Scheduled = 1,
        Completed = 2
    }

    public static class WorkflowBookmarkTypes
    {
        public const string ScheduleTaskScheduled = "ScheduleTaskScheduled";
        public const string WorkstationTaskCompleted = "WorkstationTaskCompleted";
    }
}
