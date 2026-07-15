using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities;
using MyToDo.Api.Repositories;
using MyToDo.Api.Services;
using MyToDo.Api.Services.Workflow;
using MyToDo.Api.Services.Workflow.Executors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// EF Core with SQLite
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

// Workflow bookmark service
builder.Services.AddScoped<IWorkflowBookmarkService, WorkflowBookmarkService>();

// Workstation gateway (fake local implementation)
builder.Services.AddScoped<IWorkstationGateway, FakeWorkstationGateway>();

// Node executors – all implementations of IWorkflowNodeExecutor are collected by the registry
builder.Services.AddScoped<IWorkflowNodeExecutor, StartNodeExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutor, EndNodeExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutor, ScheduleTaskNodeExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutor, WorkstationTaskNodeExecutor>();

// Executor registry (resolved via IEnumerable<IWorkflowNodeExecutor>)
builder.Services.AddScoped<WorkflowNodeExecutorRegistry>();

// Workflow runtime and APS scheduler
builder.Services.AddScoped<IWorkflowRuntime, WorkflowRuntime>();
builder.Services.AddScoped<IApsScheduler, ApsScheduler>();

// Background service: periodically runs the APS scheduling cycle
builder.Services.AddHostedService<ApsSchedulerBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

// Auto-create database schema on startup for SQLite runtime.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyToDoContext>();
    db.Database.EnsureCreated();
}

app.Run();
