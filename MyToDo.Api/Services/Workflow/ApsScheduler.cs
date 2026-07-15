using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Simplified APS scheduler:
    /// 1) Load tasks in ReadyForScheduling ordered by priority desc, earliest start asc.
    /// 2) For each task, pick matching resource type with earliest available time.
    /// 3) Create ScheduleResult and mark task as Scheduled.
    /// 
    /// This is intentionally greedy and local-optimal only, but good enough for a minimal runnable demo.
    /// </summary>
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
            var resourceAvailability = await _context.ScheduleResults
                .GroupBy(x => x.ResourceId)
                .Select(g => new { ResourceId = g.Key, NextAvailable = g.Max(x => x.EndTime) })
                .ToDictionaryAsync(x => x.ResourceId, x => x.NextAvailable, cancellationToken);

            var now = DateTime.UtcNow;
            var results = new List<ScheduleResult>();

            foreach (var task in readyTasks)
            {
                var matchedResources = resources
                    .Where(r => r.ResourceType == task.RequiredResourceType)
                    .ToList();

                if (matchedResources.Count == 0)
                {
                    continue;
                }

                var selected = matchedResources
                    .Select(resource =>
                    {
                        var hasAvailabilityData = resourceAvailability.TryGetValue(resource.Id, out var nextAvailable);
                        var candidateStart = MaxTime(task.EarliestStartTime, hasAvailabilityData ? nextAvailable : now);
                        return new { Resource = resource, Start = candidateStart };
                    })
                    .OrderBy(x => x.Start)
                    .First();

                var endTime = selected.Start.AddMinutes(task.DurationMinutes);
                resourceAvailability[selected.Resource.Id] = endTime;

                task.Status = SchedulableTaskStatus.Scheduled;
                task.ScheduledResourceId = selected.Resource.Id;
                task.ScheduledStartTime = selected.Start;
                task.ScheduledEndTime = endTime;

                var result = new ScheduleResult
                {
                    Id = Guid.NewGuid(),
                    SchedulableTaskId = task.Id,
                    ResourceId = selected.Resource.Id,
                    StartTime = selected.Start,
                    EndTime = endTime,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ScheduleResults.Add(result);
                results.Add(result);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return results;
        }

        private static DateTime MaxTime(DateTime first, DateTime second)
        {
            return first >= second ? first : second;
        }
    }
}
