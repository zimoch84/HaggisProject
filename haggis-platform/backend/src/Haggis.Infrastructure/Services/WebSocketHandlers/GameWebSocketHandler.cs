using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.Infrastructure.Dtos.GameRooms;
using Haggis.Infrastructure.Services.Application;
using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services.Models;
using Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

namespace Haggis.Infrastructure.Services.WebSocketHandlers;

public sealed class GameWebSocketHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonElement EmptyObjectPayload = JsonDocument.Parse("{}").RootElement.Clone();
    private static readonly GameWebSocketOperationParser OperationParser = new();

    private readonly IGameCommandApplicationService _applicationService;
    private readonly IGameConnectionManager _connectionManager;
    private readonly IGameRoomStore _roomStore;
    private readonly IReadOnlyDictionary<GameWebSocketOperationType, IGameOperationStrategy> _operationStrategies;

    internal IGameCommandApplicationService ApplicationService => _applicationService;
    internal IGameConnectionManager ConnectionManager => _connectionManager;
    internal IGameRoomStore RoomStore => _roomStore;

    public GameWebSocketHandler(
        IGameCommandApplicationService applicationService,
        IGameConnectionManager connectionManager,
        IGameRoomStore roomStore)
    {
        _applicationService = applicationService;
        _connectionManager = connectionManager;
        _roomStore = roomStore;
        _operationStrategies = new Dictionary<GameWebSocketOperationType, IGameOperationStrategy>
        {
            [GameWebSocketOperationType.Command] = new CommandOperationStrategy(this),
            [GameWebSocketOperationType.Join] = new JoinOperationStrategy(this),
            [GameWebSocketOperationType.Create] = new CreateOperationStrategy(this),
            [GameWebSocketOperationType.Chat] = new ChatOperationStrategy(this)
        };
    }

    public async Task HandleClientAsync(string gameId, WebSocket socket, CancellationToken cancellationToken)
    {
        var registration = _connectionManager.Register(gameId, socket);

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var text = await ReceiveTextAsync(socket, cancellationToken);
                if (text is null)
                {
                    break;
                }

                if (!OperationParser.TryParse(text, out var operation) || operation is null)
                {
                    await SendOperationErrorAsync(
                        socket,
                        "unknown",
                        gameId,
                        "Missing or unsupported operation. Use join, create, chat, command.",
                        cancellationToken);
                    continue;
                }

                var operationContext = new OperationContext(gameId, socket, registration.ClientId);
                if (_operationStrategies.TryGetValue(operation.Operation, out var strategy))
                {
                    await strategy.HandleAsync(operationContext, operation, cancellationToken);
                    continue;
                }

                await SendOperationErrorAsync(
                    socket,
                    operation.RawOperation,
                    gameId,
                    $"Unsupported operation '{operation.RawOperation}'.",
                    cancellationToken);
            }
        }
        finally
        {
            _connectionManager.Unregister(gameId, registration.ClientId);
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

    internal async Task BroadcastRoomJoinedAsync(string gameId, string joinedPlayerId, GameRoom room, CancellationToken cancellationToken)
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

    internal bool IsPlayerAllowedForGame(string gameId, string playerId)
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

        var activeGameIds = _roomStore.ListRooms()
            .Select(x => x.GameId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => _connectionManager.GetSockets(x).Count > 0)
            .ToArray();

        foreach (var activeGameId in activeGameIds)
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

    internal async Task BroadcastAsync(string gameId, string operation, object message, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var segment = new ArraySegment<byte>(bytes);

        foreach (var recipientSocket in _connectionManager.GetSockets(gameId))
        {
            if (recipientSocket.State != WebSocketState.Open)
            {
                continue;
            }

            await recipientSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    internal static async Task SendToClientAsync(WebSocket socket, string operation, object message, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    internal static Task SendOperationErrorAsync(
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

    internal static bool TryParseCommandMessage(GameWebSocketOperationDto operation, out GameClientMessage message)
    {
        message = default!;
        try
        {
            if (TryDeserializeOperationPayload(operation, out CommandPayload? operationPayload) && operationPayload?.Command is not null)
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

    internal static bool TryParseChatMessage(GameWebSocketOperationDto operation, out GameChatClientMessage message)
    {
        message = default!;
        try
        {
            if (TryDeserializeOperationPayload(operation, out ChatPayload? operationPayload) && operationPayload is not null)
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

    internal static bool TryParseJoinPayload(GameWebSocketOperationDto operation, out string playerId)
    {
        playerId = string.Empty;
        try
        {
            if (TryDeserializeOperationPayload(operation, out JoinPayload? operationPayload) && operationPayload is not null)
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

    internal static bool TryParseCreatePayload(GameWebSocketOperationDto operation, out string playerId, out JsonElement payload)
    {
        playerId = string.Empty;
        payload = EmptyObjectPayload;
        try
        {
            if (TryDeserializeOperationPayload(operation, out CreatePayload? operationPayload) && operationPayload is not null)
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

    private static bool TryDeserializeOperationPayload<TPayload>(GameWebSocketOperationDto operation, out TPayload? payload)
    {
        payload = default;

        try
        {
            if (operation.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return false;
            }

            payload = JsonSerializer.Deserialize<TPayload>(operation.Payload.GetRawText(), SerializerOptions);
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
    internal sealed record OperationContext(string GameId, WebSocket Socket, Guid ClientId);
}
