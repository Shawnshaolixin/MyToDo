using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;
using MyToDo.Api.Services.Workflow;

namespace MyToDo.Api.BackgroundServices
{
    /// <summary>
    /// Periodically checks for <see cref="ScheduleResult"/>s whose
    /// <c>EndTime</c> has elapsed (i.e. the scheduled time slot is over)
    /// and resumes any suspended workflow tokens that were waiting on them.
    ///
    /// This simulates the device completing its scheduled work on time.
    /// In a real system this would be driven by actual device events.
    ///
    /// Interval defaults to 60 seconds and can be overridden via
    /// <c>ScheduleRelease:IntervalSeconds</c> in appsettings.
    /// </summary>
    public class ScheduleReleaseBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduleReleaseBackgroundService> _logger;
        private readonly TimeSpan _interval;

        public ScheduleReleaseBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ScheduleReleaseBackgroundService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            var seconds = configuration.GetValue<int?>("ScheduleRelease:IntervalSeconds") ?? 60;
            _interval = TimeSpan.FromSeconds(seconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScheduleReleaseBackgroundService started (interval: {Interval}s).", _interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);

                try
                {
                    await RunReleaseCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during schedule-release cycle.");
                }
            }
        }

        private async Task RunReleaseCycleAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyToDoContext>();
            var runtime = scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();

            var now = DateTime.UtcNow;

            // Find schedule results that have elapsed but whose associated task
            // bookmark is still active (i.e. not yet resumed).
            var elapsed = await context.ScheduleResults
                .Where(r => r.EndTime <= now)
                .Include(r => r.SchedulableTask)
                .ToListAsync(cancellationToken);

            foreach (var result in elapsed)
            {
                if (result.SchedulableTask == null) continue;

                var bookmarkKey = result.SchedulableTask.Id.ToString();

                var activeBookmark = await context.WorkflowBookmarks
                    .FirstOrDefaultAsync(
                        b => b.BookmarkType == WorkflowBookmarkTypes.ScheduleTaskScheduled
                          && b.BookmarkKey == bookmarkKey
                          && b.Status == WorkflowBookmarkStatus.Active,
                        cancellationToken);

                if (activeBookmark != null)
                {
                    _logger.LogInformation(
                        "Releasing schedule slot for task {TaskId} (EndTime={EndTime}).",
                        result.SchedulableTaskId, result.EndTime);

                    await runtime.ResumeAsync(
                        WorkflowBookmarkTypes.ScheduleTaskScheduled,
                        bookmarkKey,
                        null,
                        cancellationToken);
                }
            }
        }
    }
}
