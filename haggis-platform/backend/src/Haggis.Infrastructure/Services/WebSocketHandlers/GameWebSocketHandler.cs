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
    private static readonly GameWebSocketOperationParser OperationParser = new();

    internal IGameCommandApplicationService ApplicationService { get; }
    internal IGameConnectionManager ConnectionManager { get; }
    internal IGameRoomStore RoomStore { get; }
    private IGameOperationStrategy<GameWebSocketCommandOperationDto> CommandStrategy { get; }
    private IGameOperationStrategy<GameWebSocketJoinOperationDto> JoinStrategy { get; }
    private IGameOperationStrategy<GameWebSocketCreateOperationDto> CreateStrategy { get; }
    private IGameOperationStrategy<GameWebSocketChatOperationDto> ChatStrategy { get; }
    private IGameOperationStrategy<GameWebSocketSnapshotOperationDto> SnapshotStrategy { get; }

    public GameWebSocketHandler(
        IGameCommandApplicationService applicationService,
        IGameConnectionManager connectionManager,
        IGameRoomStore roomStore)
    {
        ApplicationService = applicationService;
        ConnectionManager = connectionManager;
        RoomStore = roomStore;
        CommandStrategy = new CommandOperationStrategy(this);
        JoinStrategy = new JoinOperationStrategy(this);
        CreateStrategy = new CreateOperationStrategy(this);
        ChatStrategy = new ChatOperationStrategy(this);
        SnapshotStrategy = new SnapshotOperationStrategy(this);
    }

    public async Task HandleClientAsync(string gameId, WebSocket socket, CancellationToken cancellationToken)
    {
        var registration = ConnectionManager.Register(gameId, socket);  //clientId i ConnectionId

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
                        "Missing or unsupported operation. Use join, create, chat, command, snapshot.",
                        cancellationToken);
                    continue;
                }

                var operationContext = new OperationContext(gameId, socket, registration.ClientId);
                if (operation is GameWebSocketCommandOperationDto commandOperation)
                {
                    await CommandStrategy.HandleAsync(operationContext, commandOperation, cancellationToken);
                    continue;
                }

                if (operation is GameWebSocketJoinOperationDto joinOperation)
                {
                    await JoinStrategy.HandleAsync(operationContext, joinOperation, cancellationToken);
                    continue;
                }

                if (operation is GameWebSocketCreateOperationDto createOperation)
                {
                    await CreateStrategy.HandleAsync(operationContext, createOperation, cancellationToken);
                    continue;
                }

                if (operation is GameWebSocketChatOperationDto chatOperation)
                {
                    await ChatStrategy.HandleAsync(operationContext, chatOperation, cancellationToken);
                    continue;
                }

                if (operation is GameWebSocketSnapshotOperationDto snapshotOperation)
                {
                    await SnapshotStrategy.HandleAsync(operationContext, snapshotOperation, cancellationToken);
                    continue;
                }

                var rawOperation = (operation as GameWebSocketUnknownOperationDto)?.RawOperation ?? "unknown";
                await SendOperationErrorAsync(
                    socket,
                    rawOperation,
                    gameId,
                    $"Unsupported operation '{rawOperation}'.",
                    cancellationToken);
            }
        }
        finally
        {
            ConnectionManager.Unregister(gameId, registration.ClientId);
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
            messageKind = "event",
            gameId,
            playerId = joinedPlayerId,
            room = ToRoomResponse(room),
            createdAt = DateTimeOffset.UtcNow
        };

        await BroadcastAsync(gameId, "join", payload, cancellationToken);
    }

    internal bool IsPlayerAllowedForGame(string gameId, string playerId)
    {
        if (!RoomStore.TryGetRoom(gameId, out var room) || room is null)
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
                    Chat: new GameChatMessage(PlayerId: "server", Text: normalizedMessage),
                    MessageKind: "event"),
                cancellationToken);
            return;
        }

        var activeGameIds = RoomStore.ListRooms()
            .Select(x => x.GameId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => ConnectionManager.GetSockets(x).Count > 0)
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
                    Chat: new GameChatMessage(PlayerId: "server", Text: normalizedMessage),
                    MessageKind: "event"),
                cancellationToken);
        }
    }

    internal async Task BroadcastAsync(string gameId, string operation, object message, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var segment = new ArraySegment<byte>(bytes);

        foreach (var recipientSocket in ConnectionManager.GetSockets(gameId))
        {
            if (recipientSocket.State != WebSocketState.Open)
            {
                continue;
            }

            await recipientSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    internal async Task BroadcastExceptAsync(
        string gameId,
        WebSocket excludedSocket,
        string operation,
        object message,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var segment = new ArraySegment<byte>(bytes);

        foreach (var recipientSocket in ConnectionManager.GetSockets(gameId))
        {
            if (ReferenceEquals(recipientSocket, excludedSocket) || recipientSocket.State != WebSocketState.Open)
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
            messageKind = "response",
            gameId,
            error,
            createdAt = DateTimeOffset.UtcNow
        };

        return SendToClientAsync(socket, operation, message, cancellationToken);
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

    internal sealed record OperationContext(string GameId, WebSocket Socket, Guid ClientId);
}
