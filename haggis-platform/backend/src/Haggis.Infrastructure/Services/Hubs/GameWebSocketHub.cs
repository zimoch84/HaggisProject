using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.Infrastructure.Services;
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
    private readonly IPlayerSocketRegistry _playerSocketRegistry;

    public GameWebSocketHub(IGameCommandApplicationService applicationService, IPlayerSocketRegistry playerSocketRegistry)
    {
        _applicationService = applicationService;
        _playerSocketRegistry = playerSocketRegistry;
    }

    public async Task HandleClientAsync(string gameId, WebSocket socket, CancellationToken cancellationToken)
    {
        var clients = _gameClients.GetOrAdd(gameId, static _ => new ConcurrentDictionary<Guid, WebSocket>());
        var clientId = Guid.NewGuid();
        var connectionId = _playerSocketRegistry.Register(socket, $"games.actions:{gameId}");
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

                var messageType = TryParseMessageType(text);
                if (messageType is null)
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

                if (messageType.Equals("Command", StringComparison.OrdinalIgnoreCase))
                {
                    var commandMessage = TryParseCommandMessage(text);
                    if (commandMessage is null)
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

                    _playerSocketRegistry.BindPlayer(connectionId, commandMessage.Command.PlayerId);

                    var outgoing = _applicationService.Handle(gameId, commandMessage);
                    if (!outgoing.Type.Equals("CommandApplied", StringComparison.Ordinal))
                    {
                        await SendToClientAsync(socket, outgoing, cancellationToken);
                        continue;
                    }

                    await BroadcastAsync(gameId, outgoing, cancellationToken);
                    continue;
                }

                if (messageType.Equals("Chat", StringComparison.OrdinalIgnoreCase))
                {
                    var chatMessage = TryParseChatMessage(text);
                    if (chatMessage is null)
                    {
                        await SendToClientAsync(socket, new GameEventMessage(
                            Type: "ChatRejected",
                            OrderPointer: null,
                            GameId: gameId,
                            Error: "Invalid chat payload.",
                            Command: null,
                            State: null,
                            CreatedAt: DateTimeOffset.UtcNow), cancellationToken);
                        continue;
                    }

                    _playerSocketRegistry.BindPlayer(connectionId, chatMessage.Chat.PlayerId);

                    var outgoing = new GameEventMessage(
                        Type: "ChatPosted",
                        OrderPointer: null,
                        GameId: gameId,
                        Error: null,
                        Command: null,
                        State: null,
                        CreatedAt: DateTimeOffset.UtcNow,
                        Chat: new GameChatMessage(
                            PlayerId: chatMessage.Chat.PlayerId.Trim(),
                            Text: chatMessage.Chat.Text.Trim()));

                    await BroadcastAsync(gameId, outgoing, cancellationToken);
                    continue;
                }

                await SendToClientAsync(socket, new GameEventMessage(
                    Type: "CommandRejected",
                    OrderPointer: null,
                    GameId: gameId,
                    Error: $"Unsupported message type '{messageType}'.",
                    Command: null,
                    State: null,
                    CreatedAt: DateTimeOffset.UtcNow), cancellationToken);
            }
        }
        finally
        {
            clients.TryRemove(clientId, out _);
            _playerSocketRegistry.Unregister(connectionId);
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
                catch (WebSocketException)
                {
                }
                catch (IOException)
                {
                }
            }
        }
    }

    public async Task BroadcastServerAnnouncementAsync(string message, string? gameId, CancellationToken cancellationToken)
    {
        var normalizedMessage = message.Trim();
        if (normalizedMessage.Length == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(gameId))
        {
            var scoped = gameId.Trim();
            await BroadcastAsync(scoped, new GameEventMessage(
                Type: "ServerAnnouncement",
                OrderPointer: null,
                GameId: scoped,
                Error: null,
                Command: null,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow,
                Chat: new GameChatMessage(PlayerId: "server", Text: normalizedMessage)), cancellationToken);
            return;
        }

        var gameIds = _gameClients.Keys.ToArray();
        foreach (var activeGameId in gameIds)
        {
            await BroadcastAsync(activeGameId, new GameEventMessage(
                Type: "ServerAnnouncement",
                OrderPointer: null,
                GameId: activeGameId,
                Error: null,
                Command: null,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow,
                Chat: new GameChatMessage(PlayerId: "server", Text: normalizedMessage)), cancellationToken);
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

    private static string? TryParseMessageType(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("type", out var typeElement))
            {
                return null;
            }

            return typeElement.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static GameClientMessage? TryParseCommandMessage(string text)
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

    private static GameChatClientMessage? TryParseChatMessage(string text)
    {
        try
        {
            var message = JsonSerializer.Deserialize<GameChatClientMessage>(text, SerializerOptions);
            if (message is null || message.Chat is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(message.Type) || !message.Type.Equals("Chat", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(message.Chat.PlayerId) || string.IsNullOrWhiteSpace(message.Chat.Text))
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
            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch (WebSocketException)
            {
                return null;
            }

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
