using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;
using MyToDo.Api.Services.Workflow;

namespace MyToDo.Api.Tests
{
    public class WorkflowRuntimeApsTests
    {
        /// <summary>
        /// Helper that wires up the workflow runtime with all required dependencies
        /// using a shared in-memory DbContext, the real executor implementations,
        /// and FakeWorkstationGateway for local testing.
        /// </summary>
        private static (WorkflowRuntime runtime, ApsScheduler scheduler) BuildServices(MyToDoContext context)
        {
            var bookmarkService = new WorkflowBookmarkService(context);
            var gateway = new FakeWorkstationGateway();

            var registry = new WorkflowNodeExecutorRegistry();
            registry.Register(new StartNodeExecutor());
            registry.Register(new EndNodeExecutor());
            registry.Register(new ScheduleTaskExecutor(context, bookmarkService));
            registry.Register(new WorkstationTaskExecutor(gateway, bookmarkService));

            var runtime = new WorkflowRuntime(context, registry, bookmarkService);
            var scheduler = new ApsScheduler(context);
            return (runtime, scheduler);
        }

        [Fact]
        public async Task Runtime_WithApsScheduler_CompletesMinimalWorkflow()
        {
            var options = new DbContextOptionsBuilder<MyToDoContext>()
                .UseInMemoryDatabase(nameof(Runtime_WithApsScheduler_CompletesMinimalWorkflow))
                .Options;

            await using var context = new MyToDoContext(options);
            await SeedWorkflowAsync(context);

            var (runtime, scheduler) = BuildServices(context);

            var workOrder = await context.WorkOrders.SingleAsync();
            var version = await context.WorkflowVersions.SingleAsync();

            // Start: creates instance + token at Start node, advances through Start → ScheduleTask
            var instance = await runtime.StartAsync(workOrder.Id, version.Id, CancellationToken.None);

            // Verify a ScheduleTask bookmark was created
            var activeScheduleBookmark = await context.WorkflowBookmarks
                .SingleOrDefaultAsync(x => x.BookmarkType == WorkflowBookmarkTypes.ScheduleTaskScheduled && x.Status == WorkflowBookmarkStatus.Active);
            Assert.NotNull(activeScheduleBookmark);

            // Run the APS scheduler to allocate a resource and create a ScheduleResult
            var scheduleResults = await scheduler.ScheduleAsync(CancellationToken.None);
            Assert.Single(scheduleResults);

            // Resume from the scheduling bookmark → workflow advances to WorkstationTask
            await runtime.ResumeAsync(
                WorkflowBookmarkTypes.ScheduleTaskScheduled,
                scheduleResults[0].SchedulableTaskId.ToString(),
                null,
                CancellationToken.None);

            // Verify a WorkstationTask bookmark was created
            var activeWorkstationBookmark = await context.WorkflowBookmarks
                .SingleOrDefaultAsync(x => x.BookmarkType == WorkflowBookmarkTypes.WorkstationTaskCompleted && x.Status == WorkflowBookmarkStatus.Active);
            Assert.NotNull(activeWorkstationBookmark);

            // Simulate device completion: resume the workstation bookmark
            var resumed = await runtime.ResumeAsync(
                WorkflowBookmarkTypes.WorkstationTaskCompleted,
                activeWorkstationBookmark!.BookmarkKey,
                null,
                CancellationToken.None);

            Assert.True(resumed);

            // Verify the workflow and work order are both Completed
            var refreshedInstance = await context.WorkflowInstances.FirstAsync(x => x.Id == instance.Id);
            var refreshedOrder = await context.WorkOrders.FirstAsync(x => x.Id == workOrder.Id);

            Assert.Equal(WorkflowInstanceStatus.Completed, refreshedInstance.Status);
            Assert.Equal(WorkOrderStatus.Completed, refreshedOrder.Status);
        }

        private static async Task SeedWorkflowAsync(MyToDoContext context)
        {
            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = "Test Workflow",
                CreatedAt = DateTime.UtcNow
            };

            var version = new WorkflowVersion
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflow.Id,
                VersionNumber = 1,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };

            var start = new WorkflowNode
            {
                Id = Guid.NewGuid(),
                WorkflowVersionId = version.Id,
                NodeKey = "start",
                NodeType = WorkflowNodeType.Start
            };

            var schedule = new WorkflowNode
            {
                Id = Guid.NewGuid(),
                WorkflowVersionId = version.Id,
                NodeKey = "schedule",
                NodeType = WorkflowNodeType.ScheduleTask,
                RequiredResourceType = "Workstation",
                EstimatedDurationMinutes = 30
            };

            var workstation = new WorkflowNode
            {
                Id = Guid.NewGuid(),
                WorkflowVersionId = version.Id,
                NodeKey = "workstation",
                NodeType = WorkflowNodeType.WorkstationTask,
                RequiredResourceType = "Workstation"
            };

            var end = new WorkflowNode
            {
                Id = Guid.NewGuid(),
                WorkflowVersionId = version.Id,
                NodeKey = "end",
                NodeType = WorkflowNodeType.End
            };

            context.Workflows.Add(workflow);
            context.WorkflowVersions.Add(version);
            context.WorkflowNodes.AddRange(start, schedule, workstation, end);
            context.WorkflowEdges.AddRange(
                new WorkflowEdge { Id = Guid.NewGuid(), WorkflowVersionId = version.Id, FromNodeId = start.Id, ToNodeId = schedule.Id },
                new WorkflowEdge { Id = Guid.NewGuid(), WorkflowVersionId = version.Id, FromNodeId = schedule.Id, ToNodeId = workstation.Id },
                new WorkflowEdge { Id = Guid.NewGuid(), WorkflowVersionId = version.Id, FromNodeId = workstation.Id, ToNodeId = end.Id });

            context.WorkOrders.Add(new WorkOrder
            {
                Id = Guid.NewGuid(),
                WorkOrderNo = $"WO-{Guid.NewGuid():N}",
                WorkflowVersionId = version.Id,
                Priority = 10,
                EarliestStartTime = DateTime.UtcNow,
                Status = WorkOrderStatus.Submitted,
                CreatedAt = DateTime.UtcNow
            });

            context.SchedulingResources.Add(new SchedulingResource
            {
                Id = Guid.NewGuid(),
                Name = "Workstation-A",
                ResourceType = "Workstation"
            });

            await context.SaveChangesAsync();
        }
    }
}
