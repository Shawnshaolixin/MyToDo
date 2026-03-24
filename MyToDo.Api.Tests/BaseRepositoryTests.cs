using MyToDo.Api.Entities;
using MyToDo.Api.Repositories;

namespace MyToDo.Api.Tests
{
    /// <summary>
    /// Unit tests for <see cref="BaseRepository{T}"/> using the EF Core in-memory provider.
    /// </summary>
    public class BaseRepositoryTests
    {
        // ────────────────────────────────────────────────────────────────
        // GetAsync
        // ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAsync_ExistingId_ReturnsToDo()
        {
            await using var ctx = DbContextFactory.CreateAndSeed(nameof(GetAsync_ExistingId_ReturnsToDo));
            var repo = new BaseRepository<ToDo>(ctx);

            var result = await repo.GetAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("买菜", result.Title);
        }

        [Fact]
        public async Task GetAsync_NonExistingId_ReturnsNull()
        {
            await using var ctx = DbContextFactory.CreateAndSeed(nameof(GetAsync_NonExistingId_ReturnsNull));
            var repo = new BaseRepository<ToDo>(ctx);

            var result = await repo.GetAsync(999);

            Assert.Null(result);
        }

        // ────────────────────────────────────────────────────────────────
        // GetAllAsync
        // ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_NoPredicate_ReturnsAllRecords()
        {
            await using var ctx = DbContextFactory.CreateAndSeed(nameof(GetAllAsync_NoPredicate_ReturnsAllRecords));
            var repo = new BaseRepository<ToDo>(ctx);

            var result = await repo.GetAllAsync();

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_WithPredicate_ReturnsFilteredRecords()
        {
            await using var ctx = DbContextFactory.CreateAndSeed(nameof(GetAllAsync_WithPredicate_ReturnsFilteredRecords));
            var repo = new BaseRepository<ToDo>(ctx);

            var result = await repo.GetAllAsync(x => x.Status == 0);

            Assert.Equal(3, result.Count);
            Assert.All(result, item => Assert.Equal(0, item.Status));
        }

        // ────────────────────────────────────────────────────────────────
        // AddAsync
        // ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_ValidEntity_PersistsAndReturnsEntity()
        {
            await using var ctx = DbContextFactory.CreateAndSeed(nameof(AddAsync_ValidEntity_PersistsAndReturnsEntity));
            var repo = new BaseRepository<ToDo>(ctx);
            var newTodo = new ToDo { Title = "新任务", Content = "新内容", Status = 0 };

            var added = await repo.AddAsync(newTodo);

            Assert.True(added.Id > 0);
            Assert.Equal("新任务", added.Title);
            Assert.NotEqual(default, added.CreateDate);
            Assert.NotEqual(default, added.UpdateDate);

            // Confirm it was actually saved
            var all = await repo.GetAllAsync();
            Assert.Equal(6, all.Count);
        }

        // ────────────────────────────────────────────────────────────────
        // UpdateAsync
        // ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesFields()
        {
            await using var ctx = DbContextFactory.CreateAndSeed(nameof(UpdateAsync_ExistingEntity_UpdatesFields));
            var repo = new BaseRepository<ToDo>(ctx);

            var entity = await repo.GetAsync(1);
            Assert.NotNull(entity);
            entity.Title = "更新后标题";
            entity.Status = 1;

            var updated = await repo.UpdateAsync(entity);

            Assert.Equal("更新后标题", updated.Title);
            Assert.Equal(1, updated.Status);
            Assert.NotEqual(default, updated.UpdateDate);
        }

        // ────────────────────────────────────────────────────────────────
        // DeleteAsync
        // ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ExistingId_RemovesRecord()
        {
            await using var ctx = DbContextFactory.CreateAndSeed(nameof(DeleteAsync_ExistingId_RemovesRecord));
            var repo = new BaseRepository<ToDo>(ctx);

            await repo.DeleteAsync(1);

            var all = await repo.GetAllAsync();
            Assert.Equal(4, all.Count);
            Assert.DoesNotContain(all, x => x.Id == 1);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_DoesNotThrow()
        {
            await using var ctx = DbContextFactory.CreateAndSeed(nameof(DeleteAsync_NonExistingId_DoesNotThrow));
            var repo = new BaseRepository<ToDo>(ctx);

            // Should complete without exception
            await repo.DeleteAsync(999);

            var all = await repo.GetAllAsync();
            Assert.Equal(5, all.Count);
        }

        // ────────────────────────────────────────────────────────────────
        // Memo repository sanity check
        // ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_Memos_ReturnsSeededMemos()
        {
            await using var ctx = DbContextFactory.CreateAndSeed(nameof(GetAllAsync_Memos_ReturnsSeededMemos));
            var repo = new BaseRepository<Memo>(ctx);

            var result = await repo.GetAllAsync();

            Assert.Equal(3, result.Count);
        }
    }
}
