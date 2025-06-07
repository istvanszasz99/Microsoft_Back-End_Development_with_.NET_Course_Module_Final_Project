using UserManagementAPI.Models;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// In-memory user store
var users = new ConcurrentDictionary<int, User>();
var nextId = 1;

// CRUD Endpoints

// GET all users
app.MapGet("/users", () => users.Values);

// GET user by ID
app.MapGet("/users/{id:int}", (int id) =>
    users.TryGetValue(id, out var user) ? Results.Ok(user) : Results.NotFound());

// POST create user
app.MapPost("/users", (User user) =>
{
    user.Id = nextId++;
    users[user.Id] = user;
    return Results.Created($"/users/{user.Id}", user);
});

// PUT update user
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    if (!users.ContainsKey(id)) return Results.NotFound();
    updatedUser.Id = id;
    users[id] = updatedUser;
    return Results.Ok(updatedUser);
});

// DELETE user
app.MapDelete("/users/{id:int}", (int id) =>
{
    return users.TryRemove(id, out _) ? Results.NoContent() : Results.NotFound();
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
