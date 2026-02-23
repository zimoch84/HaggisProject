using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Haggis.Runtime.Chat;

public sealed class ChatRoom
{
    private readonly ConcurrentDictionary<Guid, ChatClient> _clients = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task HandleClientAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var connectionId = Guid.NewGuid();
        var client = new ChatClient(connectionId, socket);
        _clients[connectionId] = client;

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var payload = await ReceiveTextAsync(socket, cancellationToken);
                if (payload is null)
                {
                    break;
                }

                if (!TryBuildChatMessage(payload, out var message, out var error))
                {
                    await SendAsync(client, error!, cancellationToken);
                    continue;
                }

                await BroadcastAsync(message!, cancellationToken);
            }
        }
        finally
        {
            _clients.TryRemove(connectionId, out _);

            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", CancellationToken.None);
            }

            client.Dispose();
        }
    }

    private async Task BroadcastAsync(ChatMessage chatMessage, CancellationToken cancellationToken)
    {
        var deadConnections = new List<Guid>();

        foreach (var pair in _clients)
        {
            var socket = pair.Value.Socket;
            if (socket.State != WebSocketState.Open)
            {
                deadConnections.Add(pair.Key);
                continue;
            }

            try
            {
                await SendAsync(pair.Value, chatMessage, cancellationToken);
            }
            catch
            {
                deadConnections.Add(pair.Key);
            }
        }

        foreach (var deadId in deadConnections)
        {
            if (_clients.TryRemove(deadId, out var removed))
            {
                removed.Dispose();
            }
        }
    }

    private static async Task SendAsync(ChatClient client, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await client.SendLock.WaitAsync(cancellationToken);
        try
        {
            if (client.Socket.State == WebSocketState.Open)
            {
                await client.Socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
            }
        }
        finally
        {
            client.SendLock.Release();
        }
    }

    private static bool TryBuildChatMessage(
        string payload,
        out ChatMessage? chatMessage,
        out ProblemDetails? problem)
    {
        chatMessage = null;
        problem = null;

        SendChatMessageRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<SendChatMessageRequest>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            problem = BuildProblem("Invalid JSON payload.");
            return false;
        }

        if (request is null)
        {
            problem = BuildProblem("Request body is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.PlayerId))
        {
            problem = BuildProblem("Field 'playerId' is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            problem = BuildProblem("Field 'text' is required.");
            return false;
        }

        if (request.Text.Length > 500)
        {
            problem = BuildProblem("Field 'text' must be <= 500 chars.");
            return false;
        }

        chatMessage = new ChatMessage(
            Guid.NewGuid().ToString(),
            request.PlayerId.Trim(),
            request.Text,
            DateTimeOffset.UtcNow);

        return true;
    }

    private static ProblemDetails BuildProblem(string detail)
    {
        return new ProblemDetails
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail
        };
    }

    private static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                return null;
            }

            stream.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class ChatClient(Guid id, WebSocket socket) : IDisposable
    {
        public Guid Id { get; } = id;
        public WebSocket Socket { get; } = socket;
        public SemaphoreSlim SendLock { get; } = new(1, 1);

        public void Dispose()
        {
            SendLock.Dispose();
            Socket.Dispose();
        }
    }

    private sealed record SendChatMessageRequest(string PlayerId, string Text);

    private sealed record ChatMessage(string MessageId, string PlayerId, string Text, DateTimeOffset CreatedAt);
}
