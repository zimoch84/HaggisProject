using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.Infrastructure.Dtos.GameRooms;
using Haggis.Infrastructure.Services;
using Haggis.Infrastructure.Services.Application;
using Haggis.Infrastructure.Services.GameRooms;
using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Hubs;

public sealed class GameWebSocketHub
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonElement EmptyObjectPayload = JsonDocument.Parse("{}").RootElement.Clone();

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _gameClients = new();
    private readonly IGameCommandApplicationService _applicationService;
    private readonly IPlayerSocketRegistry _playerSocketRegistry;
    private readonly IGameRoomStore _roomStore;

    public GameWebSocketHub(
        IGameCommandApplicationService applicationService,
        IPlayerSocketRegistry playerSocketRegistry,
        IGameRoomStore roomStore)
    {
        _applicationService = applicationService;
        _playerSocketRegistry = playerSocketRegistry;
        _roomStore = roomStore;
    }

    public async Task HandleClientAsync(string gameId, WebSocket socket, CancellationToken cancellationToken)
    {
        var clients = _gameClients.GetOrAdd(gameId, static _ => new ConcurrentDictionary<Guid, WebSocket>());
        var clientId = Guid.NewGuid();
        var connectionId = _playerSocketRegistry.Register(socket, $"games:{gameId}");
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

                var operation = TryParseOperation(text);
                if (operation is null)
                {
                    await SendOperationErrorAsync(
                        socket,
                        "unknown",
                        gameId,
                        "Missing or unsupported operation. Use join, create, chat, command.",
                        cancellationToken);
                    continue;
                }

                if (operation.Equals("command", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseCommandMessage(text, out var commandMessage))
                    {
                        await SendOperationErrorAsync(socket, "command", gameId, "Invalid command payload.", cancellationToken);
                        continue;
                    }

                    if (!IsPlayerAllowedForGame(gameId, commandMessage.Command.PlayerId))
                    {
                        var rejected = new GameEventMessage(
                            Type: "CommandRejected",
                            OrderPointer: null,
                            GameId: gameId,
                            Error: "Player is not joined to this room.",
                            Command: commandMessage.Command,
                            State: null,
                            CreatedAt: DateTimeOffset.UtcNow);
                        await SendToClientAsync(socket, "command", rejected, cancellationToken);
                        continue;
                    }

                    _playerSocketRegistry.BindPlayer(connectionId, commandMessage.Command.PlayerId);

                    var outgoing = _applicationService.Handle(gameId, commandMessage);
                    if (!outgoing.Type.Equals("CommandApplied", StringComparison.Ordinal))
                    {
                        await SendToClientAsync(socket, "command", outgoing, cancellationToken);
                        continue;
                    }

                    await BroadcastAsync(gameId, "command", outgoing, cancellationToken);
                    continue;
                }

                if (operation.Equals("join", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseJoinPayload(text, out var playerId))
                    {
                        await SendOperationErrorAsync(socket, "join", gameId, "Invalid join payload.", cancellationToken);
                        continue;
                    }

                    GameRoom? joinedRoom;
                    if (!_roomStore.TryJoinRoom(gameId, playerId, out joinedRoom) || joinedRoom is null)
                    {
                        joinedRoom = _roomStore.GetOrCreateRoom(gameId, playerId, "haggis");
                    }

                    _playerSocketRegistry.BindPlayer(connectionId, playerId);

                    await BroadcastRoomJoinedAsync(gameId, playerId, joinedRoom, cancellationToken);
                    continue;
                }

                if (operation.Equals("create", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseCreatePayload(text, out var playerId, out var payload))
                    {
                        await SendOperationErrorAsync(socket, "create", gameId, "Invalid create payload.", cancellationToken);
                        continue;
                    }

                    GameRoom? room;
                    if (!_roomStore.TryJoinRoom(gameId, playerId, out room) || room is null)
                    {
                        room = _roomStore.GetOrCreateRoom(gameId, playerId, "haggis");
                    }

                    _playerSocketRegistry.BindPlayer(connectionId, playerId);

                    var initializeMessage = new GameClientMessage(
                        Type: "Command",
                        Command: new GameCommand(
                            Type: "Initialize",
                            PlayerId: playerId,
                            Payload: payload),
                        State: null);

                    var outgoing = _applicationService.Handle(gameId, initializeMessage);
                    if (!outgoing.Type.Equals("CommandApplied", StringComparison.Ordinal))
                    {
                        await SendToClientAsync(socket, "create", outgoing, cancellationToken);
                        continue;
                    }

                    await BroadcastAsync(gameId, "create", outgoing, cancellationToken);
                    continue;
                }

                if (operation.Equals("chat", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseChatMessage(text, out var chatMessage))
                    {
                        await SendOperationErrorAsync(socket, "chat", gameId, "Invalid chat payload.", cancellationToken);
                        continue;
                    }

                    if (!IsPlayerAllowedForGame(gameId, chatMessage.Chat.PlayerId))
                    {
                        var rejected = new GameEventMessage(
                            Type: "ChatRejected",
                            OrderPointer: null,
                            GameId: gameId,
                            Error: "Player is not joined to this room.",
                            Command: null,
                            State: null,
                            CreatedAt: DateTimeOffset.UtcNow);
                        await SendToClientAsync(socket, "chat", rejected, cancellationToken);
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

                    await BroadcastAsync(gameId, "chat", outgoing, cancellationToken);
                    continue;
                }

                await SendOperationErrorAsync(
                    socket,
                    operation,
                    gameId,
                    $"Unsupported operation '{operation}'.",
                    cancellationToken);
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

    private async Task BroadcastRoomJoinedAsync(string gameId, string joinedPlayerId, GameRoom room, CancellationToken cancellationToken)
    {
        var payload = new
        {
            type = "RoomJoined",
            gameId,
            playerId = joinedPlayerId,
            room = ToRoomResponse(room),
            createdAt = DateTimeOffset.UtcNow
        };

        await BroadcastAsync(gameId, "join", payload, cancellationToken);
    }

    private bool IsPlayerAllowedForGame(string gameId, string playerId)
    {
        if (!_roomStore.TryGetRoom(gameId, out var room) || room is null)
        {
            return false;
        }

        return room.Players.Contains(playerId, StringComparer.OrdinalIgnoreCase);
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
            await BroadcastAsync(
                scoped,
                "chat",
                new GameEventMessage(
                    Type: "ServerAnnouncement",
                    OrderPointer: null,
                    GameId: scoped,
                    Error: null,
                    Command: null,
                    State: null,
                    CreatedAt: DateTimeOffset.UtcNow,
                    Chat: new GameChatMessage(PlayerId: "server", Text: normalizedMessage)),
                cancellationToken);
            return;
        }

        var gameIds = _gameClients.Keys.ToArray();
        foreach (var activeGameId in gameIds)
        {
            await BroadcastAsync(
                activeGameId,
                "chat",
                new GameEventMessage(
                    Type: "ServerAnnouncement",
                    OrderPointer: null,
                    GameId: activeGameId,
                    Error: null,
                    Command: null,
                    State: null,
                    CreatedAt: DateTimeOffset.UtcNow,
                    Chat: new GameChatMessage(PlayerId: "server", Text: normalizedMessage)),
                cancellationToken);
        }
    }

    private async Task BroadcastAsync(string gameId, string operation, object message, CancellationToken cancellationToken)
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

    private static async Task SendToClientAsync(WebSocket socket, string operation, object message, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static Task SendOperationErrorAsync(
        WebSocket socket,
        string operation,
        string gameId,
        string error,
        CancellationToken cancellationToken)
    {
        var message = new
        {
            type = "OperationRejected",
            gameId,
            error,
            createdAt = DateTimeOffset.UtcNow
        };

        return SendToClientAsync(socket, operation, message, cancellationToken);
    }

    private static string? TryParseOperation(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (root.TryGetProperty("operation", out var operationElement))
            {
                var operation = operationElement.GetString();
                if (!string.IsNullOrWhiteSpace(operation))
                {
                    return operation.Trim();
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryParseCommandMessage(string text, out GameClientMessage message)
    {
        message = default!;
        try
        {
            if (TryDeserializeOperationPayload(text, out CommandPayload? operationPayload) && operationPayload?.Command is not null)
            {
                if (string.IsNullOrWhiteSpace(operationPayload.Command.Type) ||
                    string.IsNullOrWhiteSpace(operationPayload.Command.PlayerId))
                {
                    return false;
                }

                message = new GameClientMessage("Command", operationPayload.Command, operationPayload.State);
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseChatMessage(string text, out GameChatClientMessage message)
    {
        message = default!;
        try
        {
            if (TryDeserializeOperationPayload(text, out ChatPayload? operationPayload) && operationPayload is not null)
            {
                if (string.IsNullOrWhiteSpace(operationPayload.PlayerId) || string.IsNullOrWhiteSpace(operationPayload.Text))
                {
                    return false;
                }

                message = new GameChatClientMessage(
                    Type: "Chat",
                    Chat: new GameChatMessage(operationPayload.PlayerId.Trim(), operationPayload.Text.Trim()));
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseJoinPayload(string text, out string playerId)
    {
        playerId = string.Empty;
        try
        {
            if (TryDeserializeOperationPayload(text, out JoinPayload? operationPayload) && operationPayload is not null)
            {
                if (string.IsNullOrWhiteSpace(operationPayload.PlayerId))
                {
                    return false;
                }

                playerId = operationPayload.PlayerId.Trim();
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseCreatePayload(string text, out string playerId, out JsonElement payload)
    {
        playerId = string.Empty;
        payload = EmptyObjectPayload;
        try
        {
            if (TryDeserializeOperationPayload(text, out CreatePayload? operationPayload) && operationPayload is not null)
            {
                if (string.IsNullOrWhiteSpace(operationPayload.PlayerId))
                {
                    return false;
                }

                playerId = operationPayload.PlayerId.Trim();
                payload = operationPayload.Payload.ValueKind is JsonValueKind.Undefined
                    ? EmptyObjectPayload
                    : operationPayload.Payload.Clone();
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryDeserializeOperationPayload<TPayload>(string text, out TPayload? payload)
    {
        payload = default;

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (!root.TryGetProperty("operation", out _) || !root.TryGetProperty("payload", out var payloadElement))
            {
                return false;
            }

            payload = JsonSerializer.Deserialize<TPayload>(payloadElement.GetRawText(), SerializerOptions);
            return payload is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static GameRoomResponse ToRoomResponse(GameRoom room)
    {
        return new GameRoomResponse(
            RoomId: room.RoomId,
            GameId: room.GameId,
            GameType: room.GameType,
            RoomName: room.RoomName,
            CreatedAt: room.CreatedAt,
            Players: room.Players,
            GameEndpoint: $"/ws/games/{room.GameId}");
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

    private sealed record JoinPayload(string PlayerId);
    private sealed record CreatePayload(string PlayerId, JsonElement Payload);
    private sealed record ChatPayload(string PlayerId, string Text);
    private sealed record CommandPayload(GameCommand Command, GameStateSnapshot? State);
}
