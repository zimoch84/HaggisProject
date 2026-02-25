using System.Net.WebSockets;
using Game.API.Services.Engine;
using Game.API.Services.Hubs;
using Game.API.Services.Interfaces;
using Game.API.Services.Sessions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IGameEngine, GenericGameEngine>();
builder.Services.AddSingleton<IGameSessionStore, GameSessionStore>();
builder.Services.AddSingleton<GameWebSocketHub>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () => "Game.API is running.");

app.Map("/games/create", async (HttpContext context, GameWebSocketHub hub) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleClientAsync(Guid.NewGuid().ToString("N"), socket, context.RequestAborted);
});

app.Map("/games/{gameId}/actions", async (HttpContext context, GameWebSocketHub hub, string gameId) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleClientAsync(gameId, socket, context.RequestAborted);
});

// Backward-compatible alias for older clients.
app.Map("/ws/chat/{gameId}", async (HttpContext context, GameWebSocketHub hub, string gameId) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleClientAsync(gameId, socket, context.RequestAborted);
});

app.Map("/ws/chat/global", async (HttpContext context, GameWebSocketHub hub) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleClientAsync("global", socket, context.RequestAborted);
});

app.Run();

public partial class Program;
