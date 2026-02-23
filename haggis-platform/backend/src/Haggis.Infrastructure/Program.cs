using Haggis.Application.Engine.Loop;
using System.Net.WebSockets;
using Haggis.Infrastructure.Services.Application;
using Haggis.Infrastructure.Services.Engine;
using Haggis.Infrastructure.Services.Engine.Haggis;
using Haggis.Infrastructure.Services.Hubs;
using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services.Models;
using Haggis.Infrastructure.Services.Infrastructure.Sessions;
using Haggis.Domain.Model;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IAiMoveStrategy<HaggisGameState, HaggisAction>, HaggisAiMoveStrategy>();
builder.Services.AddSingleton<IMoveRuleValidator<HaggisGameState, HaggisAction, GameCommand>, HaggisMoveRuleValidator>();
builder.Services.AddSingleton<HaggisServerGameLoop>();
builder.Services.AddSingleton<IGameEngine, HaggisGameEngine>();
builder.Services.AddSingleton<IGameSessionStore, GameSessionStore>();
builder.Services.AddSingleton<IGameCommandApplicationService, GameCommandApplicationService>();
builder.Services.AddSingleton<GameWebSocketHub>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () => "Haggis.Infrastructure is running.");

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

app.Run();

public partial class Program;
