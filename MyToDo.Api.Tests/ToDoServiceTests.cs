using MyToDo.Api.Extensions;
using MyToDo.Api.Repositories;
using MyToDo.Api.Entities;
using MyToDo.Api.Services;
using Moq;

namespace MyToDo.Api.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ToDoService"/> using mocked repositories.
    /// </summary>
    public class ToDoServiceTests
    {
        // ── Shared seed data ──────────────────────────────────────────────────

        private static List<ToDo> SeedToDos() =>
        [
            new ToDo { Id = 1, Title = "买菜", Content = "牛奶、鸡蛋", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
            new ToDo { Id = 2, Title = "健身", Content = "跑步30分钟", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
            new ToDo { Id = 3, Title = "读书", Content = "每天读30页", Status = 1, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
            new ToDo { Id = 4, Title = "写代码", Content = "完成单元测试", Status = 1, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
            new ToDo { Id = 5, Title = "喝水", Content = "每天8杯水", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
        ];

        private static List<Memo> SeedMemos() =>
        [
            new Memo { Id = 1, Title = "购物清单", Content = "牛奶、面包", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
            new Memo { Id = 2, Title = "读书笔记", Content = "第三章要点", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
        ];

        // ── GetAllAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_NoFilter_ReturnsAllToDos()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedToDos());
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.GetAllAsync();

            Assert.True(result.Status);
            Assert.Equal(5, result.Result!.Count);
        }

        [Fact]
        public async Task GetAllAsync_WithStatusFilter_ReturnsOnlyMatchingToDos()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedToDos());
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.GetAllAsync(status: 1);

            Assert.True(result.Status);
            Assert.Equal(2, result.Result!.Count);
            Assert.All(result.Result, dto => Assert.Equal(1, dto.Status));
        }

        [Fact]
        public async Task GetAllAsync_WithSearchKeyword_ReturnsMatchingToDos()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedToDos());
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.GetAllAsync(search: "读书");

            Assert.True(result.Status);
            Assert.Single(result.Result!);
            Assert.Equal("读书", result.Result![0].Title);
        }

        [Fact]
        public async Task GetAllAsync_SearchNoMatch_ReturnsEmptyList()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedToDos());
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.GetAllAsync(search: "不存在的关键词");

            Assert.True(result.Status);
            Assert.Empty(result.Result!);
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsCorrectDto()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(SeedToDos().First(t => t.Id == 1));
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.GetByIdAsync(1);

            Assert.True(result.Status);
            Assert.NotNull(result.Result);
            Assert.Equal(1, result.Result.Id);
            Assert.Equal("买菜", result.Result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsFailure()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAsync(999)).ReturnsAsync((ToDo?)null);
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.GetByIdAsync(999);

            Assert.False(result.Status);
            Assert.Equal("数据不存在", result.Message);
        }

        // ── AddAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_ValidDto_ReturnsAddedDto()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            var dto = new ToDoDto { Title = "新任务", Content = "新内容", Status = 0 };
            var saved = new ToDo { Id = 10, Title = "新任务", Content = "新内容", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now };
            mockTodoRepo.Setup(r => r.AddAsync(It.IsAny<ToDo>())).ReturnsAsync(saved);
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.AddAsync(dto);

            Assert.True(result.Status);
            Assert.Equal("添加成功", result.Message);
            Assert.NotNull(result.Result);
            Assert.Equal(10, result.Result.Id);
            Assert.Equal("新任务", result.Result.Title);
        }

        // ── UpdateAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ExistingDto_ReturnsUpdatedDto()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            var existing = SeedToDos().First(t => t.Id == 1);
            var dto = new ToDoDto { Id = 1, Title = "更新标题", Content = "更新内容", Status = 1 };
            mockTodoRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(existing);
            mockTodoRepo.Setup(r => r.UpdateAsync(It.IsAny<ToDo>())).ReturnsAsync((ToDo e) => e);
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.UpdateAsync(dto);

            Assert.True(result.Status);
            Assert.Equal("更新成功", result.Message);
            Assert.Equal("更新标题", result.Result!.Title);
            Assert.Equal(1, result.Result.Status);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingDto_ReturnsFailure()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAsync(999)).ReturnsAsync((ToDo?)null);
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.UpdateAsync(new ToDoDto { Id = 999, Title = "x" });

            Assert.False(result.Status);
            Assert.Equal("数据不存在", result.Message);
        }

        // ── DeleteAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ValidId_ReturnsSuccess()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.DeleteAsync(1);

            Assert.True(result.Status);
            Assert.Equal("删除成功", result.Message);
            Assert.True(result.Result);
            mockTodoRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        // ── GetSummaryAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetSummaryAsync_ReturnsSummaryCounts()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedToDos());
            mockMemoRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedMemos());
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.GetSummaryAsync();

            Assert.True(result.Status);
            var summary = result.Result!;
            // 5 total, 2 completed → 3 pending
            Assert.Equal(3, summary.ToDoCount);
            Assert.Equal(2, summary.CompletedCount);
            Assert.Equal(40.0, summary.CompletedRatio);
            Assert.Equal(2, summary.MemoCount);
        }

        [Fact]
        public async Task GetSummaryAsync_NoToDos_RatioIsZero()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(new List<ToDo>());
            mockMemoRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(new List<Memo>());
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.GetSummaryAsync();

            Assert.True(result.Status);
            Assert.Equal(0, result.Result!.CompletedRatio);
        }

        [Fact]
        public async Task GetSummaryAsync_RepositoryThrows_ReturnsFailure()
        {
            var mockTodoRepo = new Mock<IBaseRepository<ToDo>>();
            var mockMemoRepo = new Mock<IBaseRepository<Memo>>();
            mockTodoRepo.Setup(r => r.GetAllAsync(null)).ThrowsAsync(new Exception("DB error"));
            var svc = new ToDoService(mockTodoRepo.Object, mockMemoRepo.Object);

            var result = await svc.GetSummaryAsync();

            Assert.False(result.Status);
            Assert.Contains("DB error", result.Message);
        }
    }
}
