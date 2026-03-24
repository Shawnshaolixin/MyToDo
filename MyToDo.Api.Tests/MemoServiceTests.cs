using MyToDo.Api.Extensions;
using MyToDo.Api.Repositories;
using MyToDo.Api.Entities;
using MyToDo.Api.Services;
using Moq;

namespace MyToDo.Api.Tests
{
    /// <summary>
    /// Unit tests for <see cref="MemoService"/> using mocked repositories.
    /// </summary>
    public class MemoServiceTests
    {
        // ── Shared seed data ──────────────────────────────────────────────────

        private static List<Memo> SeedMemos() =>
        [
            new Memo { Id = 1, Title = "购物清单", Content = "牛奶、面包、鸡蛋", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
            new Memo { Id = 2, Title = "会议记录", Content = "讨论项目进度", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
            new Memo { Id = 3, Title = "读书笔记", Content = "《深入理解计算机系统》第三章", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
        ];

        // ── GetAllAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_NoSearch_ReturnsAllMemos()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedMemos());
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.GetAllAsync();

            Assert.True(result.Status);
            Assert.Equal(3, result.Result!.Count);
        }

        [Fact]
        public async Task GetAllAsync_WithSearchKeyword_ReturnsMatchingMemos()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedMemos());
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.GetAllAsync(search: "读书");

            Assert.True(result.Status);
            Assert.Single(result.Result!);
            Assert.Equal("读书笔记", result.Result![0].Title);
        }

        [Fact]
        public async Task GetAllAsync_SearchMatchesContent_ReturnsMatchingMemos()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedMemos());
            var svc = new MemoService(mockRepo.Object);

            // "面包" is in the Content of memo 1
            var result = await svc.GetAllAsync(search: "面包");

            Assert.True(result.Status);
            Assert.Single(result.Result!);
            Assert.Equal("购物清单", result.Result![0].Title);
        }

        [Fact]
        public async Task GetAllAsync_SearchNoMatch_ReturnsEmptyList()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(SeedMemos());
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.GetAllAsync(search: "不存在的词");

            Assert.True(result.Status);
            Assert.Empty(result.Result!);
        }

        [Fact]
        public async Task GetAllAsync_RepositoryThrows_ReturnsFailure()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.GetAllAsync(null)).ThrowsAsync(new Exception("连接失败"));
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.GetAllAsync();

            Assert.False(result.Status);
            Assert.Contains("连接失败", result.Message);
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsCorrectDto()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.GetAsync(2)).ReturnsAsync(SeedMemos().First(m => m.Id == 2));
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.GetByIdAsync(2);

            Assert.True(result.Status);
            Assert.Equal(2, result.Result!.Id);
            Assert.Equal("会议记录", result.Result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsFailure()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.GetAsync(999)).ReturnsAsync((Memo?)null);
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.GetByIdAsync(999);

            Assert.False(result.Status);
            Assert.Equal("数据不存在", result.Message);
        }

        // ── AddAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_ValidDto_ReturnsAddedDto()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            var dto = new MemoDto { Title = "新备忘录", Content = "新内容", Status = 0 };
            var saved = new Memo { Id = 10, Title = "新备忘录", Content = "新内容", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now };
            mockRepo.Setup(r => r.AddAsync(It.IsAny<Memo>())).ReturnsAsync(saved);
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.AddAsync(dto);

            Assert.True(result.Status);
            Assert.Equal("添加成功", result.Message);
            Assert.Equal(10, result.Result!.Id);
            Assert.Equal("新备忘录", result.Result.Title);
        }

        [Fact]
        public async Task AddAsync_RepositoryThrows_ReturnsFailure()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.AddAsync(It.IsAny<Memo>())).ThrowsAsync(new Exception("写入失败"));
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.AddAsync(new MemoDto { Title = "x" });

            Assert.False(result.Status);
            Assert.Contains("写入失败", result.Message);
        }

        // ── UpdateAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ExistingDto_ReturnsUpdatedDto()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            var existing = SeedMemos().First(m => m.Id == 1);
            var dto = new MemoDto { Id = 1, Title = "更新后标题", Content = "更新后内容", Status = 0 };
            mockRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(existing);
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Memo>())).ReturnsAsync((Memo e) => e);
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.UpdateAsync(dto);

            Assert.True(result.Status);
            Assert.Equal("更新成功", result.Message);
            Assert.Equal("更新后标题", result.Result!.Title);
            Assert.Equal("更新后内容", result.Result.Content);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingDto_ReturnsFailure()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.GetAsync(999)).ReturnsAsync((Memo?)null);
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.UpdateAsync(new MemoDto { Id = 999, Title = "x" });

            Assert.False(result.Status);
            Assert.Equal("数据不存在", result.Message);
        }

        // ── DeleteAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ValidId_ReturnsSuccess()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.DeleteAsync(1);

            Assert.True(result.Status);
            Assert.Equal("删除成功", result.Message);
            Assert.True(result.Result);
            mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_RepositoryThrows_ReturnsFailure()
        {
            var mockRepo = new Mock<IBaseRepository<Memo>>();
            mockRepo.Setup(r => r.DeleteAsync(1)).ThrowsAsync(new Exception("删除失败"));
            var svc = new MemoService(mockRepo.Object);

            var result = await svc.DeleteAsync(1);

            Assert.False(result.Status);
            Assert.Contains("删除失败", result.Message);
        }
    }
}
