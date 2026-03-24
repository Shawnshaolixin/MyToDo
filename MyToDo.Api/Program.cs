using Microsoft.EntityFrameworkCore;
using MyToDo.Api.Context;
using MyToDo.Api.Entities;
using MyToDo.Api.Repositories;
using MyToDo.Api.Services;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyToDoContext>();
    db.Database.EnsureCreated();
}

app.Run();
