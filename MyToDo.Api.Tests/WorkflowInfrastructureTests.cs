using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;
using MyToDo.Api.Services.Workflow;

namespace MyToDo.Api.Tests
{
    public class WorkflowInfrastructureTests
    {
        [Fact]
        public async Task BookmarkService_ConsumeAsync_IsIdempotent()
        {
            var options = new DbContextOptionsBuilder<MyToDoContext>()
                .UseInMemoryDatabase(nameof(BookmarkService_ConsumeAsync_IsIdempotent))
                .Options;

            await using var context = new MyToDoContext(options);
            var service = new WorkflowBookmarkService(context);

            var bookmark = new WorkflowBookmark
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = Guid.NewGuid(),
                ExecutionTokenId = Guid.NewGuid(),
                WorkflowNodeInstanceId = Guid.NewGuid(),
                BookmarkType = WorkflowBookmarkTypes.WorkstationTaskCompleted,
                BookmarkKey = "demo",
                Status = WorkflowBookmarkStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await service.CreateAsync(bookmark, CancellationToken.None);
            var consumedFirst = await service.ConsumeAsync(bookmark.Id, CancellationToken.None);
            var consumedSecond = await service.ConsumeAsync(bookmark.Id, CancellationToken.None);

            Assert.True(consumedFirst);
            Assert.False(consumedSecond);
        }

        [Fact]
        public async Task FakeWorkstationGateway_StartExperimentAsync_ReturnsDeterministicGuid()
        {
            var gateway = new FakeWorkstationGateway();
            var parameters = new Dictionary<string, string>
            {
                ["temperature"] = "25",
                ["durationMinutes"] = "30"
            };

            var first = await gateway.StartExperimentAsync("EXP-DEMO-001", parameters, CancellationToken.None);
            var second = await gateway.StartExperimentAsync("EXP-DEMO-001", parameters, CancellationToken.None);

            Assert.True(first.Accepted);
            Assert.Equal(first.DeviceJobId, second.DeviceJobId);
        }
    }
}
