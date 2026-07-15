using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;
using MyToDo.Api.Services.Workflow;

namespace MyToDo.Api.Tests
{
    public class WorkflowRuntimeApsTests
    {
        [Fact]
        public async Task Runtime_WithApsScheduler_CompletesMinimalWorkflow()
        {
            var options = new DbContextOptionsBuilder<MyToDoContext>()
                .UseInMemoryDatabase(nameof(Runtime_WithApsScheduler_CompletesMinimalWorkflow))
                .Options;

            await using var context = new MyToDoContext(options);
            await SeedWorkflowAsync(context);

            var runtime = CreateRuntime(context);
            var scheduler = new SimpleApsScheduler(context);

            var workOrder = await context.WorkOrders.SingleAsync();
            var version = await context.WorkflowVersions.SingleAsync();

            var instance = await runtime.StartAsync(workOrder.Id, version.Id, CancellationToken.None);

            var activeScheduleBookmark = await context.WorkflowBookmarks
                .SingleOrDefaultAsync(x => x.BookmarkType == WorkflowBookmarkTypes.ScheduleTaskScheduled && x.Status == WorkflowBookmarkStatus.Active);
            Assert.NotNull(activeScheduleBookmark);

            var scheduleResults = await scheduler.ScheduleAsync(CancellationToken.None);
            Assert.Single(scheduleResults);

            await runtime.ResumeAsync(
                WorkflowBookmarkTypes.ScheduleTaskScheduled,
                scheduleResults[0].SchedulableTaskId.ToString(),
                null,
                CancellationToken.None);

            var activeWorkstationBookmark = await context.WorkflowBookmarks
                .SingleOrDefaultAsync(x => x.BookmarkType == WorkflowBookmarkTypes.WorkstationTaskCompleted && x.Status == WorkflowBookmarkStatus.Active);
            Assert.NotNull(activeWorkstationBookmark);

            var resumed = await runtime.ResumeAsync(
                WorkflowBookmarkTypes.WorkstationTaskCompleted,
                activeWorkstationBookmark!.BookmarkKey,
                null,
                CancellationToken.None);

            Assert.True(resumed);

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

        private static WorkflowRuntime CreateRuntime(MyToDoContext context)
        {
            var bookmarkService = new WorkflowBookmarkService(context);
            var registry = new WorkflowNodeExecutorRegistry(
            [
                new StartNodeExecutor(),
                new EndNodeExecutor(),
                new ScheduleTaskNodeExecutor(context),
                new WorkstationTaskExecutor(new FakeWorkstationGateway())
            ]);

            return new WorkflowRuntime(context, registry, bookmarkService);
        }
    }
}
