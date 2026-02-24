using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Serwer.API.Dtos.Chat;
using Serwer.API.Dtos.GameRooms;
using Serwer.API.Services.GameRooms;

namespace Serwer.API.Services;

public sealed class GameRoomWebSocketHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IGameRoomStore _roomStore;

    public GameRoomWebSocketHandler(IGameRoomStore roomStore)
    {
        _roomStore = roomStore;
    }

    public async Task HandleCreateRoomAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection expected.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
        {
            var raw = await ReceiveTextAsync(socket, context.RequestAborted);
            if (raw is null)
            {
                break;
            }

            var request = JsonSerializer.Deserialize<CreateGameRoomRequest>(raw, SerializerOptions);
            if (request is null || string.IsNullOrWhiteSpace(request.GameType) || string.IsNullOrWhiteSpace(request.HostPlayerId))
            {
                await SendAsync(socket, new ProblemDetailsMessage("Invalid room payload.", 400), context.RequestAborted);
                continue;
            }

            if (!request.GameType.Equals("haggis", StringComparison.OrdinalIgnoreCase))
            {
                await SendAsync(socket, new ProblemDetailsMessage($"Unsupported gameType '{request.GameType}'.", 400), context.RequestAborted);
                continue;
            }

            var room = _roomStore.CreateRoom(
                request.HostPlayerId.Trim(),
                request.GameType.Trim().ToLowerInvariant(),
                request.RoomName);

            await SendAsync(socket, ToResponse(room), context.RequestAborted);
        }
    }

    public async Task HandleListRoomsAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection expected.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
        {
            var raw = await ReceiveTextAsync(socket, context.RequestAborted);
            if (raw is null)
            {
                break;
            }

            var request = JsonSerializer.Deserialize<ListGameRoomsRequest>(raw, SerializerOptions);
            if (request is null)
            {
                await SendAsync(socket, new ProblemDetailsMessage("Invalid list rooms payload.", 400), context.RequestAborted);
                continue;
            }

            var rooms = _roomStore.ListRooms().Select(ToResponse).ToList();
            await SendAsync(socket, rooms, context.RequestAborted);
        }
    }

    public async Task HandleJoinRoomAsync(HttpContext context, string roomId)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection expected.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
        {
            var raw = await ReceiveTextAsync(socket, context.RequestAborted);
            if (raw is null)
            {
                break;
            }

            var request = JsonSerializer.Deserialize<JoinGameRoomRequest>(raw, SerializerOptions);
            if (request is null || string.IsNullOrWhiteSpace(request.PlayerId))
            {
                await SendAsync(socket, new ProblemDetailsMessage("Invalid join payload.", 400), context.RequestAborted);
                continue;
            }

            if (!_roomStore.TryJoinRoom(roomId, request.PlayerId.Trim(), out var room) || room is null)
            {
                await SendAsync(socket, new ProblemDetailsMessage("Game room not found.", 404), context.RequestAborted);
                continue;
            }

            await SendAsync(socket, ToResponse(room), context.RequestAborted);
        }
    }

    private static GameRoomResponse ToResponse(GameRoom room)
    {
        return new GameRoomResponse(
            RoomId: room.RoomId,
            GameId: room.GameId,
            GameType: room.GameType,
            RoomName: room.RoomName,
            CreatedAt: room.CreatedAt,
            Players: room.Players,
            GameEndpoint: $"/games/{room.GameId}/actions");
    }

    private static async Task SendAsync(WebSocket socket, object payload, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            ms.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }

    private sealed record ListGameRoomsRequest;
}
