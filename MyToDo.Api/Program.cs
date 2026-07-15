using Microsoft.EntityFrameworkCore;
using MyToDo.Api.BackgroundServices;
using MyToDo.Api.Context;
using MyToDo.Api.Entities;
using MyToDo.Api.Repositories;
using MyToDo.Api.Services;
using MyToDo.Api.Services.Workflow;
using MyToDo.Api.Services.Workstation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// ── EF Core with SQLite ───────────────────────────────────────────────────────
// Connection string key is "DefaultConnection" (maps to appsettings.json).
// "ToDoConnection" is kept as a fallback for backwards-compatibility.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("ToDoConnection")
    ?? "Data Source=mytodo.db";

builder.Services.AddDbContext<MyToDoContext>(options =>
    options.UseSqlite(connectionString));

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IBaseRepository<ToDo>, BaseRepository<ToDo>>();
builder.Services.AddScoped<IBaseRepository<Memo>, BaseRepository<Memo>>();

// ── Core application services ─────────────────────────────────────────────────
builder.Services.AddScoped<IToDoService, ToDoService>();
builder.Services.AddScoped<IMemoService, MemoService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();

// ── Workflow runtime & scheduling ─────────────────────────────────────────────
builder.Services.AddScoped<IWorkflowRuntime, WorkflowRuntime>();
builder.Services.AddScoped<IApsScheduler, ApsScheduler>();
builder.Services.AddScoped<IWorkflowBookmarkService, WorkflowBookmarkService>();

// ── Node executor registry (singleton — executors are stateless) ──────────────
builder.Services.AddSingleton<IWorkflowNodeExecutorRegistry>(sp =>
{
    var registry = new WorkflowNodeExecutorRegistry();
    // Register built-in executors
    registry.Register(new StartNodeExecutor());
    registry.Register(new EndNodeExecutor());
    // WorkstationTaskExecutor needs IWorkstationGateway (resolved per-request via factory)
    return registry;
});

// Register individual executors as scoped so they can access scoped dependencies
builder.Services.AddScoped<StartNodeExecutor>();
builder.Services.AddScoped<EndNodeExecutor>();
builder.Services.AddScoped<WorkstationTaskExecutor>();

// ── Workstation gateway ───────────────────────────────────────────────────────
// FakeWorkstationGateway is the default implementation for local development.
// Replace with a real HTTP gateway in production.
builder.Services.AddScoped<IWorkstationGateway, FakeWorkstationGateway>();

// ── Workstation application services ─────────────────────────────────────────
builder.Services.AddScoped<WorkstationTaskAppService>();
builder.Services.AddScoped<WorkstationEventAppService>();

// ── Background services ───────────────────────────────────────────────────────
// ApsSchedulingBackgroundService: runs IApsScheduler.ScheduleAsync on a timer
// and resumes ScheduleTaskScheduled bookmarks.
builder.Services.AddHostedService<ApsSchedulingBackgroundService>();

// ScheduleReleaseBackgroundService: resumes workflow tokens whose scheduled
// time slot has elapsed (simulates device completing on schedule).
builder.Services.AddHostedService<ScheduleReleaseBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

// ── Database initialisation ───────────────────────────────────────────────────
// EnsureCreated() creates the schema from the EF model if the database does not
// exist yet.  This is suitable for local SQLite development because it is fast
// and requires no migration files.
//
// Trade-off vs. Migrations:
//   + EnsureCreated: zero setup, always matches current model, great for demos.
//   - EnsureCreated: does NOT apply incremental schema changes — drop the db
//     file and restart if the model changes.
//   + Migrations: safe for production, supports up/down, auditable history.
//   - Migrations: require `dotnet ef migrations add` and `database update`
//     after every model change.
//
// Switch to Migrations for any environment beyond local development.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MyToDoContext>();
    // EnsureCreated is a no-op if the database already exists.
    db.Database.EnsureCreated();
}

app.Run();

