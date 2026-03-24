using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities;

namespace MyToDo.Api.Tests
{
    /// <summary>
    /// Provides a fresh in-memory MyToDoContext for each test, pre-seeded with known data.
    /// </summary>
    internal static class DbContextFactory
    {
        /// <summary>
        /// Creates an isolated in-memory context for the given test name.
        /// Each call with the same <paramref name="dbName"/> shares the same in-memory store,
        /// so pass a unique name per test (e.g. use nameof(TestMethod)).
        /// </summary>
        public static MyToDoContext Create(string dbName)
        {
            var options = new DbContextOptionsBuilder<MyToDoContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new MyToDoContext(options);
        }

        /// <summary>
        /// Seeds standard test data into the provided context and returns it.
        /// </summary>
        public static MyToDoContext CreateAndSeed(string dbName)
        {
            var ctx = Create(dbName);

            // --- ToDo seed data ---
            ctx.ToDos.AddRange(
                new ToDo { Id = 1, Title = "买菜", Content = "牛奶、鸡蛋、面包", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
                new ToDo { Id = 2, Title = "健身", Content = "跑步30分钟", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
                new ToDo { Id = 3, Title = "读书", Content = "每天读30页", Status = 1, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
                new ToDo { Id = 4, Title = "写代码", Content = "完成单元测试", Status = 1, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
                new ToDo { Id = 5, Title = "喝水", Content = "每天8杯水", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now }
            );

            // --- Memo seed data ---
            ctx.Memos.AddRange(
                new Memo { Id = 1, Title = "购物清单", Content = "牛奶、面包、鸡蛋", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
                new Memo { Id = 2, Title = "会议记录", Content = "讨论项目进度和下阶段目标", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now },
                new Memo { Id = 3, Title = "读书笔记", Content = "《深入理解计算机系统》第三章要点", Status = 0, CreateDate = DateTime.Now, UpdateDate = DateTime.Now }
            );

            ctx.SaveChanges();
            return ctx;
        }
    }
}
