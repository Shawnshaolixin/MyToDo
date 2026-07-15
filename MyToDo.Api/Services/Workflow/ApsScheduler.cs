using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Simple Advanced Planning &amp; Scheduling (APS) implementation.
    ///
    /// Simplified scheduling heuristic (greedy, single-pass):
    /// <list type="number">
    ///   <item><b>Prioritise</b>: tasks are sorted by Priority (desc) then EarliestStartTime (asc)
    ///         so high-priority and time-sensitive jobs are allocated first.</item>
    ///   <item><b>Match resources</b>: for each task, only resources whose
    ///         <see cref="SchedulingResource.ResourceType"/> matches the task's
    ///         <see cref="SchedulableTask.RequiredResourceType"/> are considered.</item>
    ///   <item><b>Earliest-finish selection</b>: among matching resources, the one whose
    ///         next-available time yields the earliest candidate start is chosen (Earliest
    ///         Due Date / Earliest Start Time rule).</item>
    ///   <item><b>In-memory availability tracking</b>: a local dictionary accumulates
    ///         end-times as tasks are assigned so the next task in the batch sees the
    ///         updated availability of already-scheduled resources.</item>
    /// </list>
    ///
    /// Known limitations:
    /// <list type="bullet">
    ///   <item>No backtracking or look-ahead — not optimal for complex job shops.</item>
    ///   <item>Does not model setup times, resource calendars, or maintenance windows.</item>
    ///   <item>All scheduling is single-threaded; concurrent ScheduleAsync calls on the
    ///         same DbContext would race.  Use a distributed lock for production.</item>
    /// </list>
    ///
    /// This class is also registered as <c>SimpleApsScheduler</c> in the DI container
    /// (via the <see cref="IApsScheduler"/> interface) to satisfy the SimpleApsScheduler
    /// requirement while preserving test compatibility.
    /// </summary>
    public class ApsScheduler : IApsScheduler
    {
        private readonly MyToDoContext _context;

        public ApsScheduler(MyToDoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Finds all <see cref="SchedulableTaskStatus.ReadyForScheduling"/> tasks, matches
        /// each to the best available resource, creates <see cref="ScheduleResult"/> records,
        /// and updates task status to <see cref="SchedulableTaskStatus.Scheduled"/>.
        /// </summary>
        public async Task<IReadOnlyList<ScheduleResult>> ScheduleAsync(CancellationToken cancellationToken)
        {
            // Load all ready tasks ordered by scheduling priority
            var readyTasks = await _context.SchedulableTasks
                .Where(x => x.Status == SchedulableTaskStatus.ReadyForScheduling)
                .OrderByDescending(x => x.Priority)       // Higher priority first
                .ThenBy(x => x.EarliestStartTime)         // Tie-break: earliest start first
                .ToListAsync(cancellationToken);

            if (readyTasks.Count == 0)
            {
                return [];
            }

            // Load all resources and pre-compute each resource's next available time
            // based on already-committed schedule results in the DB
            var resources = await _context.SchedulingResources.ToListAsync(cancellationToken);
            var resourceAvailability = await _context.ScheduleResults
                .GroupBy(x => x.ResourceId)
                .Select(g => new { ResourceId = g.Key, NextAvailable = g.Max(x => x.EndTime) })
                .ToDictionaryAsync(x => x.ResourceId, x => x.NextAvailable, cancellationToken);

            var now = DateTime.UtcNow;
            var results = new List<ScheduleResult>();

            foreach (var task in readyTasks)
            {
                // Only consider resources whose type matches the task requirement
                var matchedResources = resources
                    .Where(r => r.ResourceType == task.RequiredResourceType)
                    .ToList();

                if (matchedResources.Count == 0)
                {
                    // No resource available for this type — skip (task remains ReadyForScheduling)
                    continue;
                }

                // Select the resource that can start the task earliest
                var selected = matchedResources
                    .Select(resource =>
                    {
                        // If the resource has existing bookings, start after its last end time;
                        // otherwise start from max(now, task.EarliestStartTime)
                        var hasAvailabilityData = resourceAvailability.TryGetValue(resource.Id, out var nextAvailable);
                        var candidateStart = MaxTime(task.EarliestStartTime, hasAvailabilityData ? nextAvailable : now);
                        return new { Resource = resource, Start = candidateStart };
                    })
                    .OrderBy(x => x.Start)  // Earliest-start-time rule
                    .First();

                var endTime = selected.Start.AddMinutes(task.DurationMinutes);

                // Update in-memory availability so subsequent tasks in this batch see the new booking
                resourceAvailability[selected.Resource.Id] = endTime;

                // Persist the scheduling decision on the task
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

        /// <summary>Returns the later of two <see cref="DateTime"/> values.</summary>
        private static DateTime MaxTime(DateTime first, DateTime second)
        {
            return first >= second ? first : second;
        }
    }
}
