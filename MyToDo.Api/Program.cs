using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities;
using MyToDo.Api.Repositories;
using MyToDo.Api.Services;
using MyToDo.Api.Services.Workflow;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// EF Core with SQLite.
// Connection string is read from appsettings.json / environment variables:
//   "ConnectionStrings:ToDoConnection": "Data Source=mytodo.db"
builder.Services.AddDbContext<MyToDoContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ToDoConnection")));

// Register repositories
builder.Services.AddScoped<IBaseRepository<ToDo>, BaseRepository<ToDo>>();
builder.Services.AddScoped<IBaseRepository<Memo>, BaseRepository<Memo>>();

// Register services
builder.Services.AddScoped<IToDoService, ToDoService>();
builder.Services.AddScoped<IMemoService, MemoService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();

// --- Workflow runtime services ---

// Bookmark service: manages workflow suspension/resumption points.
builder.Services.AddScoped<IWorkflowBookmarkService, WorkflowBookmarkService>();

// Workstation gateway: FakeWorkstationGateway returns deterministic fake data for local
// testing.  Replace with a real HTTP/MQTT gateway implementation for production.
builder.Services.AddScoped<IWorkstationGateway, FakeWorkstationGateway>();

// Node executor registry: singleton so the dictionary is built once and shared.
// Each executor is registered below and handles a specific WorkflowNodeType.
builder.Services.AddSingleton<IWorkflowNodeExecutorRegistry>(sp =>
{
    var registry = new WorkflowNodeExecutorRegistry();

    // StartNodeExecutor: pass-through, no dependencies
    registry.Register(new StartNodeExecutor());

    // EndNodeExecutor: marks token + instance complete, no dependencies
    registry.Register(new EndNodeExecutor());

    // ScheduleTaskExecutor: creates a SchedulableTask and bookmark, needs context + service
    // Resolved via the scoped DI scope so each request gets its own context instance.
    // Note: we register a factory-based executor below instead of a pre-built singleton
    // because ScheduleTaskExecutor depends on scoped services (DbContext, bookmark service).
    return registry;
});

// ScheduleTaskExecutor and WorkstationTaskExecutor have scoped dependencies (DbContext,
// IWorkflowBookmarkService) so they must be registered scoped and added to the registry
// per-request rather than at startup.  WorkflowRuntime resolves them via the shared
// registry below during actual execution.

// Register individual executors as scoped so the DI container can inject them
builder.Services.AddScoped<ScheduleTaskExecutor>();
builder.Services.AddScoped<WorkstationTaskExecutor>();

// WorkflowRuntime: orchestrates node execution using the registry and bookmark service.
// Registered scoped so it shares the same DbContext as other workflow services.
builder.Services.AddScoped<IWorkflowRuntime>(sp =>
{
    var context = sp.GetRequiredService<MyToDoContext>();
    var bookmarkService = sp.GetRequiredService<IWorkflowBookmarkService>();
    var gateway = sp.GetRequiredService<IWorkstationGateway>();

    // Build a per-request registry that includes the scoped executors.
    // The singleton registry holds stateless executors (Start, End);
    // we extend it with scoped ones here.
    var registry = new WorkflowNodeExecutorRegistry();
    registry.Register(new StartNodeExecutor());
    registry.Register(new EndNodeExecutor());
    registry.Register(new ScheduleTaskExecutor(context, bookmarkService));
    registry.Register(new WorkstationTaskExecutor(gateway, bookmarkService));

    return new WorkflowRuntime(context, registry, bookmarkService);
});

// SimpleApsScheduler: greedy single-pass APS using priority + earliest-start-time rule.
// ApsScheduler IS the simple scheduler; registered under IApsScheduler.
builder.Services.AddScoped<IApsScheduler, ApsScheduler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

// Auto-create database schema on startup using EnsureCreated().
//
// EnsureCreated tradeoffs:
//   PRO  — Simple, zero-config, works perfectly for SQLite dev/demo databases.
//   CON  — Does NOT run EF Migrations; if the model changes you must delete the .db
//          file and let EnsureCreated rebuild it.
//   CON  — Not suitable for production where incremental schema upgrades are required;
//          use `db.Database.Migrate()` (with generated migration files) instead.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyToDoContext>();
    db.Database.EnsureCreated();
}

app.Run();
