using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities.Workflow;
using MyToDo.Api.Services.Workflow;
using MyToDo.Api.Services.Workstation;

namespace MyToDo.Api.Tests
{
    /// <summary>
    /// Tests for the new workflow runtime components:
    ///   - WorkflowBookmarkService
    ///   - WorkflowNodeExecutorRegistry + StartNodeExecutor + EndNodeExecutor
    ///   - FakeWorkstationGateway
    /// </summary>
    public class WorkflowComponentsTests
    {
        // ── WorkflowBookmarkService ───────────────────────────────────────────

        [Fact]
        public async Task BookmarkService_CreateAndFind_ReturnsActiveBookmark()
        {
            await using var ctx = CreateContext(nameof(BookmarkService_CreateAndFind_ReturnsActiveBookmark));
            var service = new WorkflowBookmarkService(ctx);

            var instanceId = Guid.NewGuid();
            var tokenId    = Guid.NewGuid();
            var nodeInstId = Guid.NewGuid();

            var created = await service.CreateAsync(
                instanceId, tokenId, nodeInstId,
                WorkflowBookmarkTypes.WorkstationTaskCompleted,
                "key-123",
                CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal(WorkflowBookmarkStatus.Active, created.Status);

            var found = await service.FindAsync(
                WorkflowBookmarkTypes.WorkstationTaskCompleted, "key-123", CancellationToken.None);

            Assert.NotNull(found);
            Assert.Equal(created.Id, found!.Id);
        }

        [Fact]
        public async Task BookmarkService_Consume_MarksAsConsumed()
        {
            await using var ctx = CreateContext(nameof(BookmarkService_Consume_MarksAsConsumed));
            var service = new WorkflowBookmarkService(ctx);

            var bookmark = await service.CreateAsync(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                WorkflowBookmarkTypes.ScheduleTaskScheduled,
                "sched-key",
                CancellationToken.None);

            var consumed = await service.ConsumeAsync(bookmark.Id, CancellationToken.None);
            Assert.True(consumed);

            // Second consume must return false (idempotent)
            var doubleConsume = await service.ConsumeAsync(bookmark.Id, CancellationToken.None);
            Assert.False(doubleConsume);

            var inDb = await ctx.WorkflowBookmarks.SingleAsync(b => b.Id == bookmark.Id);
            Assert.Equal(WorkflowBookmarkStatus.Consumed, inDb.Status);
            Assert.NotNull(inDb.ConsumedAt);
        }

        [Fact]
        public async Task BookmarkService_FindNonExistent_ReturnsNull()
        {
            await using var ctx = CreateContext(nameof(BookmarkService_FindNonExistent_ReturnsNull));
            var service = new WorkflowBookmarkService(ctx);

            var result = await service.FindAsync("NoSuchType", "no-such-key", CancellationToken.None);
            Assert.Null(result);
        }

        // ── WorkflowNodeExecutorRegistry ─────────────────────────────────────

        [Fact]
        public void ExecutorRegistry_Register_AndRetrieve_Works()
        {
            var registry = new WorkflowNodeExecutorRegistry();
            registry.Register(new StartNodeExecutor());
            registry.Register(new EndNodeExecutor());

            Assert.NotNull(registry.GetExecutor(WorkflowNodeType.Start));
            Assert.NotNull(registry.GetExecutor(WorkflowNodeType.End));
            Assert.Null(registry.GetExecutor(WorkflowNodeType.ScheduleTask));
        }

        [Fact]
        public void ExecutorRegistry_LaterRegistration_Overwrites()
        {
            var registry = new WorkflowNodeExecutorRegistry();
            var first  = new StartNodeExecutor();
            var second = new StartNodeExecutor();

            registry.Register(first);
            registry.Register(second);

            Assert.Same(second, registry.GetExecutor(WorkflowNodeType.Start));
        }

        // ── StartNodeExecutor ─────────────────────────────────────────────────

        [Fact]
        public async Task StartNodeExecutor_ReturnsDone()
        {
            await using var ctx = CreateContext(nameof(StartNodeExecutor_ReturnsDone));
            var executor = new StartNodeExecutor();
            var context  = BuildContext(ctx, WorkflowNodeType.Start);

            var result = await executor.ExecuteAsync(context, CancellationToken.None);

            Assert.Equal(NodeExecutionOutcome.Done, result.Outcome);
        }

        // ── EndNodeExecutor ───────────────────────────────────────────────────

        [Fact]
        public async Task EndNodeExecutor_ReturnsDone()
        {
            await using var ctx = CreateContext(nameof(EndNodeExecutor_ReturnsDone));
            var executor = new EndNodeExecutor();
            var context  = BuildContext(ctx, WorkflowNodeType.End);

            var result = await executor.ExecuteAsync(context, CancellationToken.None);

            Assert.Equal(NodeExecutionOutcome.Done, result.Outcome);
        }

        // ── FakeWorkstationGateway ────────────────────────────────────────────

        [Fact]
        public async Task FakeGateway_GetExperiments_ReturnsList()
        {
            var gateway = new FakeWorkstationGateway();
            var experiments = await gateway.GetExperimentsAsync("WS001", CancellationToken.None);

            Assert.NotEmpty(experiments);
            Assert.All(experiments, e => Assert.False(string.IsNullOrEmpty(e.Id)));
        }

        [Fact]
        public async Task FakeGateway_StartExperiment_ReturnsSuccess()
        {
            var gateway = new FakeWorkstationGateway();
            var result = await gateway.StartExperimentAsync(
                "WS001", "EXP-001", "{}", CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(string.IsNullOrEmpty(result.DeviceJobId));
        }

        [Fact]
        public async Task FakeGateway_ApplyResolution_ReturnsSuccess()
        {
            var gateway = new FakeWorkstationGateway();
            var result = await gateway.ApplyResolutionAsync(
                "WS001", "job-1", "ConfirmSample", "Ok", CancellationToken.None);

            Assert.True(result.Success);
        }

        // ── NodeExecutionResult factory methods ───────────────────────────────

        [Fact]
        public void NodeExecutionResult_Done_HasDoneOutcome()
        {
            var r = NodeExecutionResult.Done();
            Assert.Equal(NodeExecutionOutcome.Done, r.Outcome);
        }

        [Fact]
        public void NodeExecutionResult_Waiting_HasBookmarkFields()
        {
            var r = NodeExecutionResult.Waiting("TypeA", "KeyB");
            Assert.Equal(NodeExecutionOutcome.Waiting, r.Outcome);
            Assert.Equal("TypeA", r.BookmarkType);
            Assert.Equal("KeyB", r.BookmarkKey);
        }

        [Fact]
        public void NodeExecutionResult_Failed_HasErrorMessage()
        {
            var r = NodeExecutionResult.Failed("oops");
            Assert.Equal(NodeExecutionOutcome.Failed, r.Outcome);
            Assert.Equal("oops", r.ErrorMessage);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static MyToDoContext CreateContext(string name)
        {
            var options = new DbContextOptionsBuilder<MyToDoContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new MyToDoContext(options);
        }

        private static WorkflowNodeExecutionContext BuildContext(MyToDoContext ctx, WorkflowNodeType nodeType)
        {
            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                WorkOrderId = Guid.NewGuid(),
                WorkflowVersionId = Guid.NewGuid(),
                Status = WorkflowInstanceStatus.Running,
                StartedAt = DateTime.UtcNow
            };
            var token = new WorkflowExecutionToken
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instance.Id,
                CurrentNodeId = Guid.NewGuid(),
                Status = WorkflowExecutionTokenStatus.Ready,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var node = new WorkflowNode
            {
                Id = token.CurrentNodeId,
                WorkflowVersionId = instance.WorkflowVersionId,
                NodeKey = nodeType.ToString().ToLowerInvariant(),
                NodeType = nodeType
            };
            var nodeInstance = new WorkflowNodeInstance
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instance.Id,
                WorkflowNodeId = node.Id,
                ExecutionTokenId = token.Id,
                Status = WorkflowNodeInstanceStatus.Running,
                StartedAt = DateTime.UtcNow
            };

            return new WorkflowNodeExecutionContext
            {
                Instance = instance,
                Token = token,
                Node = node,
                NodeInstance = nodeInstance,
                DbContext = ctx
            };
        }
    }
}
