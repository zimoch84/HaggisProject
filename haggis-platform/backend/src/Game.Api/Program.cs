using System.Net.WebSockets;
using Game.API.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<GameWebSocketHub>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () => "Game.API is running.");

app.Map("/ws/games/{gameId}", async (HttpContext context, GameWebSocketHub hub, string gameId) =>
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

app.Run();

public partial class Program;
