using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.Infrastructure.Services.Application;
using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Hubs;

public sealed class GameWebSocketHub
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _gameClients = new();
    private readonly IGameCommandApplicationService _applicationService;

    public GameWebSocketHub(IGameCommandApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

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

                var message = TryParseClientMessage(text);
                if (message is null)
                {
                    await SendToClientAsync(socket, new GameEventMessage(
                        Type: "CommandRejected",
                        OrderPointer: null,
                        GameId: gameId,
                        Error: "Invalid command payload.",
                        Command: null,
                        State: null,
                        CreatedAt: DateTimeOffset.UtcNow), cancellationToken);
                    continue;
                }

                var outgoing = _applicationService.Handle(gameId, message);
                if (!outgoing.Type.Equals("CommandApplied", StringComparison.Ordinal))
                {
                    await SendToClientAsync(socket, outgoing, cancellationToken);
                    continue;
                }

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
            var recipientSocket = pair.Value;
            if (recipientSocket.State != WebSocketState.Open)
            {
                continue;
            }

            await recipientSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    private static async Task SendToClientAsync(WebSocket socket, GameEventMessage message, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static GameClientMessage? TryParseClientMessage(string text)
    {
        try
        {
            var message = JsonSerializer.Deserialize<GameClientMessage>(text, SerializerOptions);
            if (message is null || message.Command is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(message.Type) || !message.Type.Equals("Command", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(message.Command.Type) || string.IsNullOrWhiteSpace(message.Command.PlayerId))
            {
                return null;
            }

            return message;
        }
        catch (JsonException)
        {
            return null;
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
}
