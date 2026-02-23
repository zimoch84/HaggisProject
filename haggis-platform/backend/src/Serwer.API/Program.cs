using System.Net.WebSockets;
using Serwer.API.Services;
using Serwer.API.Services.GameRooms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<GlobalChatHub>();
builder.Services.AddSingleton<IGameRoomStore, GameRoomStore>();
builder.Services.AddSingleton<RoomChatHub>();

var app = builder.Build();
var toResponse = (GameRoom room) => new GameRoomResponse(
    RoomId: room.RoomId,
    GameId: room.GameId,
    GameType: room.GameType,
    CreatedAt: room.CreatedAt,
    Players: room.Players,
    GameEndpoint: $"/games/{room.GameId}/actions");

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () => "Serwer.API is running.");

app.MapPost("/api/gamerooms", (CreateGameRoomRequest request, IGameRoomStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.GameType) || string.IsNullOrWhiteSpace(request.HostPlayerId))
    {
        return Results.BadRequest(new { title = "Invalid room payload.", status = 400 });
    }

    if (!request.GameType.Equals("haggis", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { title = $"Unsupported gameType '{request.GameType}'.", status = 400 });
    }

    var room = store.CreateRoom(request.HostPlayerId.Trim(), request.GameType.Trim().ToLowerInvariant());
    var response = toResponse(room);

    return Results.Created($"/api/gamerooms/{room.RoomId}", response);
});

app.MapGet("/api/gamerooms", (IGameRoomStore store) =>
{
    var rooms = store.ListRooms().Select(toResponse);
    return Results.Ok(rooms);
});

app.MapPost("/api/gamerooms/{roomId}/join", (string roomId, JoinGameRoomRequest request, IGameRoomStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.PlayerId))
    {
        return Results.BadRequest(new { title = "Invalid join payload.", status = 400 });
    }

    if (!store.TryJoinRoom(roomId, request.PlayerId.Trim(), out var room) || room is null)
    {
        return Results.NotFound(new { title = "Game room not found.", status = 404 });
    }

    return Results.Ok(toResponse(room));
});

app.Map("/ws/chat/global", async (HttpContext context, GlobalChatHub hub) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleClientAsync(socket, context.RequestAborted);
});

app.Map("/ws/chat/rooms/{roomId}", async (HttpContext context, RoomChatHub hub, IGameRoomStore roomStore, string roomId) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    if (!roomStore.TryGetRoom(roomId, out _))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Game room not found.");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleClientAsync(roomId, socket, context.RequestAborted);
});

app.Run();

public partial class Program;

public sealed record CreateGameRoomRequest(string GameType, string HostPlayerId);
public sealed record JoinGameRoomRequest(string PlayerId);
public sealed record GameRoomResponse(
    string RoomId,
    string GameId,
    string GameType,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Players,
    string GameEndpoint);
