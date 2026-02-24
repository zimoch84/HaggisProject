using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Serwer.API.Dtos.Chat;
using Serwer.API.Services.GameRooms;

namespace Serwer.API.Services;

public sealed class RoomChatHub
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _roomClients = new();
    private readonly IGameRoomStore _roomStore;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RoomChatHub(IGameRoomStore roomStore)
    {
        _roomStore = roomStore;
    }

    public async Task HandleClientAsync(string roomId, WebSocket socket, CancellationToken cancellationToken)
    {
        var clients = _roomClients.GetOrAdd(roomId, static _ => new ConcurrentDictionary<Guid, WebSocket>());
        var clientId = Guid.NewGuid();
        clients[clientId] = socket;

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var raw = await ReceiveTextAsync(socket, cancellationToken);
                if (raw is null)
                {
                    break;
                }

                var request = JsonSerializer.Deserialize<SendRoomChatMessageRequest>(raw, SerializerOptions);
                if (request is null || string.IsNullOrWhiteSpace(request.PlayerId) || string.IsNullOrWhiteSpace(request.Text))
                {
                    await SendAsync(socket, new ProblemDetailsMessage("Invalid chat payload.", 400), cancellationToken);
                    continue;
                }

                if (!_roomStore.TryGetRoom(roomId, out var room) || room is null)
                {
                    await SendAsync(socket, new ProblemDetailsMessage("Game room not found.", 404), cancellationToken);
                    continue;
                }

                var isJoined = room.Players.Contains(request.PlayerId, StringComparer.OrdinalIgnoreCase);
                if (!isJoined)
                {
                    await SendAsync(socket, new ProblemDetailsMessage("Player is not joined to this room.", 403), cancellationToken);
                    continue;
                }

                var message = new RoomChatMessage(
                    MessageId: Guid.NewGuid().ToString("N"),
                    RoomId: roomId,
                    PlayerId: request.PlayerId,
                    Text: request.Text.Trim(),
                    CreatedAt: DateTimeOffset.UtcNow);

                await BroadcastToRoomAsync(roomId, message, cancellationToken);
            }
        }
        finally
        {
            clients.TryRemove(clientId, out _);
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", CancellationToken.None);
            }
        }
    }

    private async Task BroadcastToRoomAsync(string roomId, RoomChatMessage payload, CancellationToken cancellationToken)
    {
        if (!_roomClients.TryGetValue(roomId, out var clients))
        {
            return;
        }

        foreach (var pair in clients)
        {
            if (pair.Value.State != WebSocketState.Open)
            {
                continue;
            }

            await SendAsync(pair.Value, payload, cancellationToken);
        }
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

}

