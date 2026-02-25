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
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IAiMoveStrategy<HaggisGameState, HaggisAction>, HaggisAiMoveStrategy>();
builder.Services.AddSingleton<IMoveRuleValidator<HaggisGameState, HaggisAction, GameCommand>, HaggisMoveRuleValidator>();
builder.Services.AddSingleton<HaggisServerGameLoop>();
builder.Services.AddSingleton<IGameEngine, HaggisGameEngine>();
builder.Services.AddSingleton<IGameSessionStore, GameSessionStore>();
builder.Services.AddSingleton<IGameCommandApplicationService, GameCommandApplicationService>();
builder.Services.AddSingleton<GameWebSocketHub>();
builder.Services.AddSingleton<IGameRoomStore, GameRoomStore>();
builder.Services.AddSingleton<GlobalChatHub>();
builder.Services.AddSingleton<ChatWebSocketHandler>();
builder.Services.AddSingleton<GameRoomWebSocketHandler>();
builder.Services.AddSingleton<IPlayerSocketRegistry, PlayerSocketRegistry>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () => "Haggis.Infrastructure is running.");
app.MapGet("/admin/online-players", (IPlayerSocketRegistry registry, HttpContext context) =>
{
    if (!TryAuthorizeAdmin(context, builder.Configuration))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsync("Unauthorized.");
    }

    return context.Response.WriteAsJsonAsync(registry.GetOnlinePlayerConnectionCounts());
});

app.Map("/ws/rooms/create", (HttpContext context, GameRoomWebSocketHandler handler) =>
    handler.HandleCreateRoomAsync(context));

app.Map("/ws/rooms/list", (HttpContext context, GameRoomWebSocketHandler handler) =>
    handler.HandleListRoomsAsync(context));

app.Map("/ws/rooms/{roomId}/join", (HttpContext context, string roomId, GameRoomWebSocketHandler handler) =>
    handler.HandleJoinRoomAsync(context, roomId));

app.Map("/ws/chat/global", (HttpContext context, ChatWebSocketHandler handler) =>
    handler.HandleGlobalChatAsync(context));

app.Map("/games/create", async (HttpContext context, GameWebSocketHub hub) =>
{
    if (IsAdminTokenConfigured(builder.Configuration) && !TryAuthorizeAdmin(context, builder.Configuration))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized.");
        return;
    }

    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    try
    {
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await hub.HandleClientAsync(Guid.NewGuid().ToString("N"), socket, context.RequestAborted);
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

app.MapPost("/admin/broadcast", async (HttpContext context, GameWebSocketHub hub) =>
{
    if (!TryAuthorizeAdmin(context, builder.Configuration))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized.");
        return;
    }

    AdminBroadcastRequest? request;
    try
    {
        request = await JsonSerializer.DeserializeAsync<AdminBroadcastRequest>(context.Request.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, context.RequestAborted);
    }
    catch (JsonException)
    {
        request = null;
    }

    if (request is null || string.IsNullOrWhiteSpace(request.Message))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Invalid broadcast payload.");
        return;
    }

    await hub.BroadcastServerAnnouncementAsync(request.Message, request.GameId, context.RequestAborted);

    context.Response.StatusCode = StatusCodes.Status202Accepted;
    await context.Response.WriteAsJsonAsync(new
    {
        status = "accepted",
        scope = string.IsNullOrWhiteSpace(request.GameId) ? "all-games" : "single-game",
        gameId = request.GameId
    });
});

app.MapPost("/admin/kick", async (HttpContext context, IPlayerSocketRegistry registry) =>
{
    if (!TryAuthorizeAdmin(context, builder.Configuration))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized.");
        return;
    }

    AdminKickRequest? request;
    try
    {
        request = await JsonSerializer.DeserializeAsync<AdminKickRequest>(context.Request.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, context.RequestAborted);
    }
    catch (JsonException)
    {
        request = null;
    }

    if (request is null || string.IsNullOrWhiteSpace(request.PlayerId))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Invalid kick payload.");
        return;
    }

    var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Kicked by server administrator." : request.Reason.Trim();
    var kickedConnections = await registry.KickPlayerAsync(request.PlayerId, reason, context.RequestAborted);

    await context.Response.WriteAsJsonAsync(new
    {
        status = "ok",
        playerId = request.PlayerId.Trim(),
        kickedConnections
    });
});

app.Map("/games/{gameId}/actions", async (HttpContext context, GameWebSocketHub hub, string gameId) =>
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

static bool IsAdminTokenConfigured(IConfiguration configuration)
{
    var token = configuration["GAME_ADMIN_TOKEN"];
    return !string.IsNullOrWhiteSpace(token);
}

static bool TryAuthorizeAdmin(HttpContext context, IConfiguration configuration)
{
    var configuredToken = configuration["GAME_ADMIN_TOKEN"];
    if (string.IsNullOrWhiteSpace(configuredToken))
    {
        return false;
    }

    var providedToken = GetProvidedAdminToken(context);
    return !string.IsNullOrWhiteSpace(providedToken) && string.Equals(providedToken, configuredToken, StringComparison.Ordinal);
}

static string? GetProvidedAdminToken(HttpContext context)
{
    var authHeader = context.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return authHeader["Bearer ".Length..].Trim();
    }

    var adminTokenHeader = context.Request.Headers["X-Admin-Token"].ToString();
    if (!string.IsNullOrWhiteSpace(adminTokenHeader))
    {
        return adminTokenHeader.Trim();
    }

    return null;
}

public partial class Program;
