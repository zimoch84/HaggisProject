using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Game.API.Services;

public sealed class GameWebSocketHub
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _gameClients = new();
    private readonly ConcurrentDictionary<string, long> _orderPointers = new();

    public async Task HandleClientAsync(string gameId, WebSocket socket, CancellationToken cancellationToken)
    {
        var clients = _gameClients.GetOrAdd(gameId, static _ => new ConcurrentDictionary<Guid, WebSocket>());
        var clientId = Guid.NewGuid();
        clients[clientId] = socket;

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var text = await ReceiveTextAsync(socket, cancellationToken);
                if (text is null)
                {
                    break;
                }

                var orderPointer = _orderPointers.AddOrUpdate(gameId, 1, static (_, current) => current + 1);
                var outgoing = new GameEventMessage(
                    Type: "PlayerAction",
                    OrderPointer: orderPointer,
                    GameId: gameId,
                    Payload: text,
                    CreatedAt: DateTimeOffset.UtcNow);

                await BroadcastAsync(gameId, outgoing, cancellationToken);
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

    private async Task BroadcastAsync(string gameId, GameEventMessage message, CancellationToken cancellationToken)
    {
        if (!_gameClients.TryGetValue(gameId, out var clients))
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var segment = new ArraySegment<byte>(bytes);

        foreach (var pair in clients)
        {
            var socket = pair.Value;
            if (socket.State != WebSocketState.Open)
            {
                continue;
            }

            await socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }
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

    private sealed record GameEventMessage(
        string Type,
        long OrderPointer,
        string GameId,
        string Payload,
        DateTimeOffset CreatedAt);
}
