using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public interface IApsScheduler
    {
        Task<IReadOnlyList<ScheduleResult>> ScheduleAsync(CancellationToken cancellationToken);
    }
}
