using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Hosted background service that periodically runs the APS scheduling cycle.
    /// It finds all <see cref="SchedulableTaskStatus.ReadyForScheduling"/> tasks,
    /// schedules them via <see cref="IApsScheduler"/>, then resumes the matching
    /// workflow bookmarks so the engine can continue execution.
    /// </summary>
    public class ApsSchedulerBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApsSchedulerBackgroundService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        public ApsSchedulerBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ApsSchedulerBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("APS Scheduler background service started.");
            while (!stoppingToken.IsCancellationRequested)
            {
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
                    _logger.LogError(ex, "Error in APS scheduling cycle.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
            _logger.LogInformation("APS Scheduler background service stopped.");
        }

        private async Task RunSchedulingCycleAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IApsScheduler>();
            var runtime = scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();

            var results = await scheduler.ScheduleAsync(cancellationToken);
            foreach (var result in results)
            {
                await runtime.ResumeAsync(
                    WorkflowBookmarkTypes.ScheduleTaskScheduled,
                    result.SchedulableTaskId.ToString(),
                    result,
                    cancellationToken);
            }

            if (results.Count > 0)
            {
                _logger.LogInformation("APS scheduling cycle completed: scheduled {Count} task(s).", results.Count);
            }
        }
    }
}
