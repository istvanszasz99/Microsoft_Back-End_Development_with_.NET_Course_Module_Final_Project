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
var nextIdLock = new object();

// Helper: Validate user
bool IsValidUser(User user, out string error)
{
    if (string.IsNullOrWhiteSpace(user.FirstName)) { error = "First name is required."; return false; }
    if (string.IsNullOrWhiteSpace(user.LastName)) { error = "Last name is required."; return false; }
    if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@")) { error = "Valid email is required."; return false; }
    if (string.IsNullOrWhiteSpace(user.Department)) { error = "Department is required."; return false; }
    error = null!;
    return true;
}

// CRUD Endpoints

// GET all users
app.MapGet("/users", () =>
{
    try
    {
        return Results.Ok(users.Values);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving users: {ex.Message}");
    }
});

// GET user by ID
app.MapGet("/users/{id:int}", (int id) =>
{
    try
    {
        return users.TryGetValue(id, out var user) ? Results.Ok(user) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving user: {ex.Message}");
    }
});

// POST create user
app.MapPost("/users", (User user) =>
{
    try
    {
        if (!IsValidUser(user, out var error))
            return Results.BadRequest(new { error });
        lock (nextIdLock)
        {
            user.Id = nextId++;
        }
        users[user.Id] = user;
        return Results.Created($"/users/{user.Id}", user);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error creating user: {ex.Message}");
    }
});

// PUT update user
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    try
    {
        if (!users.ContainsKey(id)) return Results.NotFound();
        if (!IsValidUser(updatedUser, out var error))
            return Results.BadRequest(new { error });
        updatedUser.Id = id;
        users[id] = updatedUser;
        return Results.Ok(updatedUser);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error updating user: {ex.Message}");
    }
});

// DELETE user
app.MapDelete("/users/{id:int}", (int id) =>
{
    try
    {
        return users.TryRemove(id, out _) ? Results.NoContent() : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error deleting user: {ex.Message}");
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
