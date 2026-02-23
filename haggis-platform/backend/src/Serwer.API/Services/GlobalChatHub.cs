using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Serwer.API.Services;

public sealed class GlobalChatHub
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task HandleClientAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid();
        _clients[clientId] = socket;

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var raw = await ReceiveTextAsync(socket, cancellationToken);
                if (raw is null)
                {
                    break;
                }

                var request = JsonSerializer.Deserialize<SendChatMessageRequest>(raw, SerializerOptions);
                if (request is null || string.IsNullOrWhiteSpace(request.PlayerId) || string.IsNullOrWhiteSpace(request.Text))
                {
                    var error = new ProblemDetailsMessage("Invalid chat payload.", 400);
                    await SendAsync(socket, error, cancellationToken);
                    continue;
                }

                var message = new ChatMessage(
                    MessageId: Guid.NewGuid().ToString("N"),
                    PlayerId: request.PlayerId,
                    Text: request.Text.Trim(),
                    CreatedAt: DateTimeOffset.UtcNow);

                await BroadcastAsync(message, cancellationToken);
            }
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", CancellationToken.None);
            }
        }
    }

    private async Task BroadcastAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        foreach (var pair in _clients)
        {
            var socket = pair.Value;
            if (socket.State != WebSocketState.Open)
            {
                continue;
            }

            await SendAsync(socket, message, cancellationToken);
        }
    }

    private static async Task SendAsync(WebSocket socket, object payload, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var segment = new ArraySegment<byte>(bytes);
        await socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
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

    private sealed record SendChatMessageRequest(string PlayerId, string Text);
    private sealed record ChatMessage(string MessageId, string PlayerId, string Text, DateTimeOffset CreatedAt);
    private sealed record ProblemDetailsMessage(string Title, int Status);
}
