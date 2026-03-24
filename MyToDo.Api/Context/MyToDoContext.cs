using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Entities;

namespace MyToDo.Api.Context
{
    public class MyToDoContext : DbContext
    {
        public MyToDoContext(DbContextOptions<MyToDoContext> options) : base(options) { }

        public DbSet<ToDo> ToDos { get; set; }
        public DbSet<Memo> Memos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ToDo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Content).HasMaxLength(500);
            });

            modelBuilder.Entity<Memo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Content).HasMaxLength(500);
            });
        }
    }
}
