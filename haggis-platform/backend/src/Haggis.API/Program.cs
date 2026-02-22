using Game.Core.Engine.Loop;
using System.Net.WebSockets;
using Haggis.API.Services.Engine;
using Haggis.API.Services.Engine.Haggis;
using Haggis.API.Services.Hubs;
using Haggis.API.Services.Interfaces;
using Haggis.API.Services.Models;
using Haggis.API.Services.Sessions;
using Haggis.Model;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IAiMoveStrategy<HaggisGameState, HaggisAction>, HaggisAiMoveStrategy>();
builder.Services.AddSingleton<IMoveRuleValidator<HaggisGameState, HaggisAction, GameCommand>, HaggisMoveRuleValidator>();
builder.Services.AddSingleton<HaggisServerGameLoop>();
builder.Services.AddSingleton<IGameEngine, HaggisGameEngine>();
builder.Services.AddSingleton<IGameSessionStore, GameSessionStore>();
builder.Services.AddSingleton<GameWebSocketHub>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () => "Haggis.API is running.");

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
