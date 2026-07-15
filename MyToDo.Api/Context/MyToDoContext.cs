using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Entities;
using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Context
{
    public class MyToDoContext : DbContext
    {
        public MyToDoContext(DbContextOptions<MyToDoContext> options) : base(options) { }

        public DbSet<ToDo> ToDos { get; set; }
        public DbSet<Memo> Memos { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<WorkflowVersion> WorkflowVersions { get; set; }
        public DbSet<WorkflowNode> WorkflowNodes { get; set; }
        public DbSet<WorkflowEdge> WorkflowEdges { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
        public DbSet<WorkflowExecutionToken> WorkflowExecutionTokens { get; set; }
        public DbSet<WorkflowNodeInstance> WorkflowNodeInstances { get; set; }
        public DbSet<WorkflowBookmark> WorkflowBookmarks { get; set; }
        public DbSet<SchedulingResource> SchedulingResources { get; set; }
        public DbSet<SchedulableTask> SchedulableTasks { get; set; }
        public DbSet<ScheduleResult> ScheduleResults { get; set; }

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

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.Property(e => e.Password).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Email).HasMaxLength(200);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Description).HasMaxLength(200);
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(200);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });
                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(e => e.RoleId);
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => new { e.RoleId, e.PermissionId });
                entity.HasOne(e => e.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(e => e.RoleId);
                entity.HasOne(e => e.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(e => e.PermissionId);
            });

            modelBuilder.Entity<Workflow>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasIndex(e => e.Name);
            });

            modelBuilder.Entity<WorkflowVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasIndex(e => new { e.WorkflowId, e.VersionNumber }).IsUnique();
                entity.HasOne(e => e.Workflow)
                    .WithMany(w => w.Versions)
                    .HasForeignKey(e => e.WorkflowId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WorkflowNode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NodeKey).IsRequired().HasMaxLength(128);
                entity.Property(e => e.NodeType).HasConversion<string>().IsRequired();
                entity.Property(e => e.RequiredResourceType).HasMaxLength(64);
                entity.HasIndex(e => new { e.WorkflowVersionId, e.NodeKey }).IsUnique();
                entity.HasOne(e => e.WorkflowVersion)
                    .WithMany(v => v.Nodes)
                    .HasForeignKey(e => e.WorkflowVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WorkflowEdge>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.WorkflowVersionId, e.FromNodeId, e.ToNodeId }).IsUnique();
                entity.HasOne(e => e.WorkflowVersion)
                    .WithMany(v => v.Edges)
                    .HasForeignKey(e => e.WorkflowVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.FromNode)
                    .WithMany(n => n.OutgoingEdges)
                    .HasForeignKey(e => e.FromNodeId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ToNode)
                    .WithMany(n => n.IncomingEdges)
                    .HasForeignKey(e => e.ToNodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.WorkOrderNo).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Status).HasConversion<string>().IsRequired();
                entity.HasIndex(e => e.WorkOrderNo).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasOne(e => e.WorkflowVersion)
                    .WithMany()
                    .HasForeignKey(e => e.WorkflowVersionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkflowInstance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<string>().IsRequired();
                entity.HasIndex(e => new { e.WorkOrderId, e.Status });
                entity.HasOne(e => e.WorkOrder)
                    .WithMany(o => o.WorkflowInstances)
                    .HasForeignKey(e => e.WorkOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.WorkflowVersion)
                    .WithMany()
                    .HasForeignKey(e => e.WorkflowVersionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkflowExecutionToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<string>().IsRequired();
                entity.HasIndex(e => new { e.WorkflowInstanceId, e.Status });
                entity.HasOne(e => e.WorkflowInstance)
                    .WithMany(i => i.ExecutionTokens)
                    .HasForeignKey(e => e.WorkflowInstanceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WorkflowNodeInstance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<string>().IsRequired();
                entity.HasIndex(e => new { e.WorkflowInstanceId, e.WorkflowNodeId });
                entity.HasOne(e => e.WorkflowInstance)
                    .WithMany(i => i.NodeInstances)
                    .HasForeignKey(e => e.WorkflowInstanceId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<WorkflowNode>()
                    .WithMany()
                    .HasForeignKey(e => e.WorkflowNodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkflowBookmark>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BookmarkType).IsRequired().HasMaxLength(64);
                entity.Property(e => e.BookmarkKey).IsRequired().HasMaxLength(128);
                entity.Property(e => e.Status).HasConversion<string>().IsRequired();
                entity.HasIndex(e => new { e.BookmarkType, e.BookmarkKey, e.Status });
                entity.HasOne(e => e.WorkflowInstance)
                    .WithMany(i => i.Bookmarks)
                    .HasForeignKey(e => e.WorkflowInstanceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SchedulingResource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
                entity.Property(e => e.ResourceType).IsRequired().HasMaxLength(64);
                entity.HasIndex(e => new { e.ResourceType, e.Name });
            });

            modelBuilder.Entity<SchedulableTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequiredResourceType).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Status).HasConversion<string>().IsRequired();
                entity.HasIndex(e => new { e.Status, e.Priority, e.EarliestStartTime });
                entity.HasOne(e => e.WorkOrder)
                    .WithMany()
                    .HasForeignKey(e => e.WorkOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.WorkflowInstance)
                    .WithMany()
                    .HasForeignKey(e => e.WorkflowInstanceId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.WorkflowNodeInstance)
                    .WithMany()
                    .HasForeignKey(e => e.WorkflowNodeInstanceId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ScheduledResource)
                    .WithMany()
                    .HasForeignKey(e => e.ScheduledResourceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ScheduleResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ResourceId, e.StartTime, e.EndTime });
                entity.HasOne(e => e.SchedulableTask)
                    .WithMany(t => t.ScheduleResults)
                    .HasForeignKey(e => e.SchedulableTaskId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Resource)
                    .WithMany()
                    .HasForeignKey(e => e.ResourceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
