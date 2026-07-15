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

// EF Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("ToDoConnection")
    ?? "Data Source=mytodo.db";

builder.Services.AddDbContext<MyToDoContext>(options =>
    options.UseSqlite(connectionString));

// Register repositories
builder.Services.AddScoped<IBaseRepository<ToDo>, BaseRepository<ToDo>>();
builder.Services.AddScoped<IBaseRepository<Memo>, BaseRepository<Memo>>();

// Register services
builder.Services.AddScoped<IToDoService, ToDoService>();
builder.Services.AddScoped<IMemoService, MemoService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IWorkflowRuntime, WorkflowRuntime>();
builder.Services.AddScoped<IWorkflowBookmarkService, WorkflowBookmarkService>();
builder.Services.AddScoped<IWorkflowNodeExecutor, StartNodeExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutor, EndNodeExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutor, WorkstationTaskExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutor, ScheduleTaskNodeExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutorRegistry, WorkflowNodeExecutorRegistry>();
builder.Services.AddSingleton<IWorkstationGateway, FakeWorkstationGateway>();
builder.Services.AddScoped<IApsScheduler, SimpleApsScheduler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

// Auto-create database schema on startup for SQLite runtime.
if (app.Environment.IsDevelopment())
{
    // For local/dev bootstrap only. Production should use EF migrations.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MyToDoContext>();
    db.Database.EnsureCreated();
}

app.Run();
