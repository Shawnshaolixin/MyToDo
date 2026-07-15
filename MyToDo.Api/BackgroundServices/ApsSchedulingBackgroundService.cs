using MyToDo.Api.Services.Workflow;

namespace MyToDo.Api.BackgroundServices
{
    /// <summary>
    /// Periodically invokes <see cref="IApsScheduler.ScheduleAsync"/> to assign
    /// resources to <see cref="Entities.Workflow.SchedulableTask"/>s that are
    /// ready for scheduling, then resumes the corresponding workflow bookmarks.
    ///
    /// The interval defaults to 30 seconds and can be overridden via
    /// <c>ApsScheduler:IntervalSeconds</c> in appsettings.
    /// </summary>
    public class ApsSchedulingBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ApsSchedulingBackgroundService> _logger;
        private readonly TimeSpan _interval;

        public ApsSchedulingBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ApsSchedulingBackgroundService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            var seconds = configuration.GetValue<int?>("ApsScheduler:IntervalSeconds") ?? 30;
            _interval = TimeSpan.FromSeconds(seconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ApsSchedulingBackgroundService started (interval: {Interval}s).", _interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);

                try
                {
                    await RunSchedulingCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during APS scheduling cycle.");
                }
            }
        }

        private async Task RunSchedulingCycleAsync(CancellationToken cancellationToken)
        {
            // Background services must create their own scope because IWorkflowRuntime
            // and IApsScheduler are registered as Scoped.
            using var scope = _scopeFactory.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IApsScheduler>();
            var runtime = scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();

            var results = await scheduler.ScheduleAsync(cancellationToken);

            foreach (var result in results)
            {
                _logger.LogInformation(
                    "Scheduled task {TaskId} on resource {ResourceId} from {Start} to {End}.",
                    result.SchedulableTaskId, result.ResourceId, result.StartTime, result.EndTime);

                await runtime.ResumeAsync(
                    Entities.Workflow.WorkflowBookmarkTypes.ScheduleTaskScheduled,
                    result.SchedulableTaskId.ToString(),
                    result,
                    cancellationToken);
            }
        }
    }
}
