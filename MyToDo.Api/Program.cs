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
var sqliteConnectionString = builder.Configuration.GetConnectionString("ToDoConnection")
    ?? throw new InvalidOperationException("Connection string 'ToDoConnection' is not configured.");

builder.Services.AddDbContext<MyToDoContext>(options =>
    options.UseSqlite(sqliteConnectionString));

// Register repositories
builder.Services.AddScoped<IBaseRepository<ToDo>, BaseRepository<ToDo>>();
builder.Services.AddScoped<IBaseRepository<Memo>, BaseRepository<Memo>>();

// Register services
builder.Services.AddScoped<IToDoService, ToDoService>();
builder.Services.AddScoped<IMemoService, MemoService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IWorkflowBookmarkService, WorkflowBookmarkService>();
builder.Services.AddScoped<IWorkflowNodeExecutor, StartNodeExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutor, ScheduleTaskNodeExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutor, WorkstationTaskExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutor, EndNodeExecutor>();
builder.Services.AddScoped<IWorkflowNodeExecutorRegistry, WorkflowNodeExecutorRegistry>();
builder.Services.AddSingleton<IWorkstationGateway, FakeWorkstationGateway>();
builder.Services.AddScoped<IWorkflowRuntime, WorkflowRuntime>();
builder.Services.AddScoped<IApsScheduler, SimpleApsScheduler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // EnsureCreated is convenient for local demos because it bootstraps schema automatically.
    // Tradeoff: it bypasses migration history, so production environments should use EF migrations instead.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MyToDoContext>();
    db.Database.EnsureCreated();
}

app.Run();
