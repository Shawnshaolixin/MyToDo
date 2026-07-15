using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public class SimpleApsScheduler : IApsScheduler
    {
        private readonly MyToDoContext _context;

        public SimpleApsScheduler(MyToDoContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ScheduleResult>> ScheduleAsync(CancellationToken cancellationToken)
        {
            var readyTasks = await _context.SchedulableTasks
                .Where(x => x.Status == SchedulableTaskStatus.ReadyForScheduling)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.EarliestStartTime)
                .ToListAsync(cancellationToken);

            if (readyTasks.Count == 0)
            {
                return [];
            }

            var resources = await _context.SchedulingResources.ToListAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var scheduleResults = new List<ScheduleResult>();

            foreach (var task in readyTasks)
            {
                var resource = resources.FirstOrDefault(x => x.ResourceType == task.RequiredResourceType);
                if (resource == null)
                {
                    continue;
                }

                var plannedStart = now;
                var plannedEnd = plannedStart.AddMinutes(task.DurationMinutes);

                var result = new ScheduleResult
                {
                    Id = Guid.NewGuid(),
                    SchedulableTaskId = task.Id,
                    ResourceId = resource.Id,
                    StartTime = plannedStart,
                    EndTime = plannedEnd,
                    CreatedAt = now
                };

                task.Status = SchedulableTaskStatus.Scheduled;
                task.ScheduledResourceId = resource.Id;
                task.ScheduledStartTime = plannedStart;
                task.ScheduledEndTime = plannedEnd;

                _context.ScheduleResults.Add(result);
                scheduleResults.Add(result);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return scheduleResults;
        }
    }
}
