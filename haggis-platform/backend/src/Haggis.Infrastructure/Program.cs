using Haggis.Application.Engine.Loop;
using System.Net.WebSockets;
using Haggis.Infrastructure.Services.Application;
using Haggis.Infrastructure.Services.Engine;
using Haggis.Infrastructure.Services.Engine.Haggis;
using Haggis.Infrastructure.Services.GameRooms;
using Haggis.Infrastructure.Services.Hubs;
using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services;
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
builder.Services.AddSingleton<IGameRoomStore, GameRoomStore>();
builder.Services.AddSingleton<IGlobalChatHistoryStore, InMemoryGlobalChatHistoryStore>();
builder.Services.AddSingleton<GlobalChatHub>();
builder.Services.AddSingleton<ChatWebSocketHandler>();
builder.Services.AddSingleton<IPlayerSocketRegistry, PlayerSocketRegistry>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () => "Haggis.Infrastructure is running.");

app.Map("/ws/global/chat", (HttpContext context, ChatWebSocketHandler handler) =>
    handler.HandleGlobalChatAsync(context));

app.Map("/ws/games/{gameId}", async (HttpContext context, GameWebSocketHub hub, string gameId) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    try
    {
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await hub.HandleClientAsync(gameId, socket, context.RequestAborted);
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
    }
    catch (ObjectDisposedException)
    {
    }
    catch (IOException)
    {
    }
});

app.Run();

public partial class Program;
