using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.Infrastructure.Dtos.Chat;
using Haggis.Infrastructure.Dtos.GameRooms;
using Haggis.Infrastructure.Services.GameRooms;

namespace Haggis.Infrastructure.Services;

public sealed class GlobalChatHub
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private readonly IPlayerSocketRegistry _playerSocketRegistry;
    private readonly IGlobalChatHistoryStore _historyStore;
    private readonly IGameRoomStore _roomStore;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GlobalChatHub(
        IPlayerSocketRegistry playerSocketRegistry,
        IGlobalChatHistoryStore historyStore,
        IGameRoomStore roomStore)
    {
        _playerSocketRegistry = playerSocketRegistry;
        _historyStore = historyStore;
        _roomStore = roomStore;
    }

    public GlobalChatHub(IPlayerSocketRegistry playerSocketRegistry)
        : this(playerSocketRegistry, new InMemoryGlobalChatHistoryStore(), new GameRoomStore())
    {
    }

    public async Task HandleClientAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid();
        var connectionId = _playerSocketRegistry.Register(socket, "global.chat");
        _clients[clientId] = socket;

        try
        {
            await SendBootstrapAsync(socket, cancellationToken);

            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var raw = await ReceiveTextAsync(socket, cancellationToken);
                if (raw is null)
                {
                    break;
                }

                var operation = TryParseOperation(raw);
                if (operation is null)
                {
                    await SendProblemDetailsAsync(
                        socket,
                        "Invalid chat payload.",
                        cancellationToken);
                    continue;
                }

                if (operation.Equals("chat", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseChatPayload(raw, out var request))
                    {
                        await SendProblemDetailsAsync(socket, "Invalid chat payload.", cancellationToken);
                        continue;
                    }

                    _playerSocketRegistry.BindPlayer(connectionId, request.PlayerId);

                    var message = new ChatMessage(
                        MessageId: Guid.NewGuid().ToString("N"),
                        PlayerId: request.PlayerId.Trim(),
                        Text: request.Text.Trim(),
                        CreatedAt: DateTimeOffset.UtcNow);

                    _historyStore.Append(message);
                    await BroadcastChatAsync(message, cancellationToken);
                    continue;
                }

                if (operation.Equals("listroom", StringComparison.OrdinalIgnoreCase))
                {
                    await SendRoomListAsync(socket, cancellationToken);
                    continue;
                }

                if (operation.Equals("createroom", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseCreateRoomPayload(raw, out var createPayload))
                    {
                        await SendOperationErrorAsync(socket, "createroom", "Invalid createroom payload.", cancellationToken);
                        continue;
                    }

                    var roomId = string.IsNullOrWhiteSpace(createPayload.RoomId)
                        ? Guid.NewGuid().ToString("N")
                        : createPayload.RoomId.Trim();
                    var gameType = string.IsNullOrWhiteSpace(createPayload.GameType) ? "haggis" : createPayload.GameType.Trim();
                    var roomName = string.IsNullOrWhiteSpace(createPayload.RoomName) ? null : createPayload.RoomName.Trim();
                    var room = _roomStore.GetOrCreateRoom(roomId, createPayload.PlayerId.Trim(), gameType, roomName);

                    _playerSocketRegistry.BindPlayer(connectionId, createPayload.PlayerId);

                    await SendAsync(
                        socket,
                        new
                        {
                            operation = "createroom",
                            data = new
                            {
                                room = ToRoomResponse(room),
                                gameEndpoint = $"/ws/games/{room.GameId}",
                                createdAt = DateTimeOffset.UtcNow
                            }
                        },
                        cancellationToken);
                    continue;
                }

                if (operation.Equals("privatechat", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParsePrivateChatPayload(raw, out var privatePayload))
                    {
                        await SendOperationErrorAsync(socket, "privatechat", "Invalid privatechat payload.", cancellationToken);
                        continue;
                    }

                    var roomId = string.IsNullOrWhiteSpace(privatePayload.RoomId)
                        ? Guid.NewGuid().ToString("N")
                        : privatePayload.RoomId.Trim();
                    var roomName = string.IsNullOrWhiteSpace(privatePayload.RoomName)
                        ? $"Private chat: {privatePayload.PlayerId.Trim()} and {privatePayload.TargetPlayerId.Trim()}"
                        : privatePayload.RoomName.Trim();

                    var room = _roomStore.GetOrCreateRoom(roomId, privatePayload.PlayerId.Trim(), "haggis", roomName);
                    _roomStore.TryJoinRoom(roomId, privatePayload.TargetPlayerId.Trim(), out room);

                    _playerSocketRegistry.BindPlayer(connectionId, privatePayload.PlayerId);

                    await SendAsync(
                        socket,
                        new
                        {
                            operation = "privatechat",
                            data = new
                            {
                                room = ToRoomResponse(room ?? _roomStore.GetOrCreateRoom(roomId, privatePayload.PlayerId.Trim(), "haggis", roomName)),
                                gameEndpoint = $"/ws/games/{roomId}",
                                createdAt = DateTimeOffset.UtcNow
                            }
                        },
                        cancellationToken);
                    continue;
                }

                await SendOperationErrorAsync(socket, operation, $"Unsupported operation '{operation}'.", cancellationToken);
            }
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            _playerSocketRegistry.Unregister(connectionId);
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", CancellationToken.None);
            }
        }
    }

    private async Task SendBootstrapAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var roomChannels = _roomStore.ListRooms()
            .Select(room => new ChatChannelSnapshot(
                ChannelId: $"room:{room.RoomId}",
                ChannelType: "room",
                RoomId: room.RoomId,
                RoomName: room.RoomName,
                GameType: room.GameType))
            .ToList();

        var channels = new List<ChatChannelSnapshot>
        {
            new("global", "global")
        };
        channels.AddRange(roomChannels);

        var bootstrap = new GlobalChatBootstrapMessage(
            Type: "GlobalChatBootstrap",
            Channels: channels,
            History: _historyStore.GetRecent(),
            CreatedAt: DateTimeOffset.UtcNow);

        await SendAsync(socket, bootstrap, cancellationToken);
    }

    private async Task SendRoomListAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        await SendAsync(
            socket,
            new
            {
                operation = "listroom",
                data = new
                {
                    rooms = _roomStore.ListRooms().Select(ToRoomResponse).ToList(),
                    createdAt = DateTimeOffset.UtcNow
                }
            },
            cancellationToken);
    }

    private async Task BroadcastChatAsync(ChatMessage message, CancellationToken cancellationToken)
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

    private static Task SendProblemDetailsAsync(
        WebSocket socket,
        string error,
        CancellationToken cancellationToken)
    {
        return SendAsync(socket, new ProblemDetailsMessage(error, 400), cancellationToken);
    }

    private static Task SendOperationErrorAsync(
        WebSocket socket,
        string operation,
        string error,
        CancellationToken cancellationToken)
    {
        return SendAsync(
            socket,
            new
            {
                operation,
                data = new
                {
                    type = "OperationRejected",
                    error,
                    createdAt = DateTimeOffset.UtcNow
                }
            },
            cancellationToken);
    }

    private static async Task SendAsync(WebSocket socket, object payload, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var segment = new ArraySegment<byte>(bytes);
        await socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
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

    private static bool TryParseChatPayload(string text, out SendChatMessageRequest request)
    {
        request = default!;
        try
        {
            if (TryDeserializeOperationPayload(text, out SendChatMessageRequest? operationPayload) && operationPayload is not null)
            {
                if (string.IsNullOrWhiteSpace(operationPayload.PlayerId) || string.IsNullOrWhiteSpace(operationPayload.Text))
                {
                    return false;
                }

                request = operationPayload;
                return true;
            }
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseCreateRoomPayload(string text, out CreateRoomPayload payload)
    {
        payload = default!;
        try
        {
            if (!TryDeserializeOperationPayload(text, out CreateRoomPayload? operationPayload) || operationPayload is null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(operationPayload.PlayerId))
            {
                return false;
            }

            payload = operationPayload;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParsePrivateChatPayload(string text, out PrivateChatPayload payload)
    {
        payload = default!;
        try
        {
            if (!TryDeserializeOperationPayload(text, out PrivateChatPayload? operationPayload) || operationPayload is null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(operationPayload.PlayerId) || string.IsNullOrWhiteSpace(operationPayload.TargetPlayerId))
            {
                return false;
            }

            payload = operationPayload;
            return true;
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

    private sealed record CreateRoomPayload(string PlayerId, string? GameType, string? RoomName, string? RoomId);
    private sealed record PrivateChatPayload(string PlayerId, string TargetPlayerId, string? RoomName, string? RoomId);
}
