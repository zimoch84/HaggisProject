using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.Infrastructure.Dtos.Chat;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class RealtimeOperationIntegrationTests
{
    [Test]
    public async Task GlobalChat_NewEndpoint_WhenConnect_ReturnsBootstrap()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/global/chat"), CancellationToken.None);

        var bootstrapPayload = await ReceiveTextAsync(socket, CancellationToken.None);
        var bootstrap = JsonSerializer.Deserialize<GlobalChatBootstrapMessage>(bootstrapPayload);

        Assert.That(bootstrap, Is.Not.Null);
        Assert.That(bootstrap!.Type, Is.EqualTo("GlobalChatBootstrap"));
        Assert.That(bootstrap.Channels.Any(x => x.ChannelId == "global"), Is.True);
    }

    [Test]
    public async Task Game_NewEndpoint_WhenNotWebSocket_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/games/game-op-1");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
    }

    [Test]
    public async Task GameOperation_JoinCreateCommandChat_HappyPath()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = timeoutCts.Token;
        const string gameId = "op-happy";

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();
        using var socketA = await wsClientA.ConnectAsync(new Uri($"ws://localhost/ws/games/{gameId}"), cancellationToken);
        using var socketB = await wsClientB.ConnectAsync(new Uri($"ws://localhost/ws/games/{gameId}"), cancellationToken);

        await SendJsonAsync(socketA, new { operation = "join", payload = new { playerId = "alice" } }, cancellationToken);
        _ = await ReceiveByTypeAsync(socketA, "RoomJoined", cancellationToken);

        await SendJsonAsync(socketB, new { operation = "join", payload = new { playerId = "bob" } }, cancellationToken);
        _ = await ReceiveByTypeAsync(socketA, "RoomJoined", cancellationToken);
        _ = await ReceiveByTypeAsync(socketB, "RoomJoined", cancellationToken);

        await SendJsonAsync(socketA, new
        {
            operation = "create",
            payload = new
            {
                playerId = "alice",
                payload = new
                {
                    players = new[] { "alice", "bob", "carol" },
                    seed = 123
                }
            }
        }, cancellationToken);

        var createdA = await ReceiveByTypeAsync(socketA, "CommandApplied", cancellationToken);
        var createdB = await ReceiveByTypeAsync(socketB, "CommandApplied", cancellationToken);
        Assert.That(GetRequiredPropertyIgnoreCase(createdA, "Type").GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(GetRequiredPropertyIgnoreCase(createdB, "Type").GetString(), Is.EqualTo("CommandApplied"));

        await SendJsonAsync(socketA, new
        {
            operation = "command",
            payload = new
            {
                command = new
                {
                    type = "Sync",
                    playerId = "alice",
                    payload = new
                    {
                        state = new
                        {
                            phase = "round",
                            step = 1
                        }
                    }
                }
            }
        }, cancellationToken);

        var commandA = await ReceiveByTypeAsync(socketA, "CommandApplied", cancellationToken);
        var commandB = await ReceiveByTypeAsync(socketB, "CommandApplied", cancellationToken);
        Assert.That(GetRequiredPropertyIgnoreCase(commandA, "OrderPointer").GetInt64(), Is.EqualTo(2));
        Assert.That(GetRequiredPropertyIgnoreCase(commandB, "OrderPointer").GetInt64(), Is.EqualTo(2));

        await SendJsonAsync(socketB, new
        {
            operation = "chat",
            payload = new
            {
                playerId = "bob",
                text = "hej"
            }
        }, cancellationToken);

        var chatA = await ReceiveByTypeAsync(socketA, "ChatPosted", cancellationToken);
        var chatB = await ReceiveByTypeAsync(socketB, "ChatPosted", cancellationToken);
        Assert.That(GetRequiredPropertyIgnoreCase(GetRequiredPropertyIgnoreCase(chatA, "Chat"), "PlayerId").GetString(), Is.EqualTo("bob"));
        Assert.That(GetRequiredPropertyIgnoreCase(GetRequiredPropertyIgnoreCase(chatB, "Chat"), "Text").GetString(), Is.EqualTo("hej"));
    }

    [Test]
    public async Task GlobalOperation_ListRoom_ReturnsRooms()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/global/chat"), cancellationToken);
        _ = await ReceiveTextAsync(socket, cancellationToken); // bootstrap

        await SendJsonAsync(socket, new
        {
            operation = "createroom",
            payload = new
            {
                playerId = "alice",
                roomId = "listroom-1",
                roomName = "Room For Listing"
            }
        }, cancellationToken);
        _ = await ReceiveTextAsync(socket, cancellationToken);

        await SendJsonAsync(socket, new { operation = "listroom" }, cancellationToken);
        var listPayload = await ReceiveTextAsync(socket, cancellationToken);

        using var listDoc = JsonDocument.Parse(listPayload);
        Assert.That(GetRequiredPropertyIgnoreCase(listDoc.RootElement, "operation").GetString(), Is.EqualTo("listroom"));
        var rooms = GetRequiredPropertyIgnoreCase(GetRequiredPropertyIgnoreCase(listDoc.RootElement, "data"), "rooms");
        Assert.That(rooms.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(rooms.EnumerateArray().Any(x => GetRequiredPropertyIgnoreCase(x, "roomId").GetString() == "listroom-1"), Is.True);
    }

    [Test]
    public async Task GlobalOperation_CreateRoom_ReturnsReachableGameEndpoint()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var globalClient = factory.Server.CreateWebSocketClient();
        using var globalSocket = await globalClient.ConnectAsync(new Uri("ws://localhost/ws/global/chat"), cancellationToken);
        _ = await ReceiveTextAsync(globalSocket, cancellationToken); // bootstrap

        await SendJsonAsync(globalSocket, new
        {
            operation = "createroom",
            payload = new
            {
                playerId = "alice",
                roomId = "room-link-1",
                roomName = "Link Room"
            }
        }, cancellationToken);

        var createPayload = await ReceiveTextAsync(globalSocket, cancellationToken);
        using var createDoc = JsonDocument.Parse(createPayload);
        var data = GetRequiredPropertyIgnoreCase(createDoc.RootElement, "data");
        var gameEndpoint = GetRequiredPropertyIgnoreCase(data, "gameEndpoint").GetString();
        Assert.That(gameEndpoint, Is.EqualTo("/ws/games/room-link-1"));

        var gameClient = factory.Server.CreateWebSocketClient();
        using var gameSocket = await gameClient.ConnectAsync(new Uri($"ws://localhost{gameEndpoint}"), cancellationToken);
        await SendJsonAsync(gameSocket, new { operation = "join", payload = new { playerId = "alice" } }, cancellationToken);

        var joined = await ReceiveByTypeAsync(gameSocket, "RoomJoined", cancellationToken);
        Assert.That(GetRequiredPropertyIgnoreCase(joined, "gameId").GetString(), Is.EqualTo("room-link-1"));
    }

    [Test]
    public async Task GlobalOperation_CreateRoom_ReturnsRoomObjectWithSingleCreatorPlayer()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var globalClient = factory.Server.CreateWebSocketClient();
        using var globalSocket = await globalClient.ConnectAsync(new Uri("ws://localhost/ws/global/chat"), cancellationToken);
        _ = await ReceiveTextAsync(globalSocket, cancellationToken); // bootstrap

        await SendJsonAsync(globalSocket, new
        {
            operation = "createroom",
            payload = new
            {
                playerId = "alice",
                roomId = "room-single-player-1",
                roomName = "Single Player Room"
            }
        }, cancellationToken);

        var createPayload = await ReceiveTextAsync(globalSocket, cancellationToken);
        using var createDoc = JsonDocument.Parse(createPayload);
        var data = GetRequiredPropertyIgnoreCase(createDoc.RootElement, "data");
        var room = GetRequiredPropertyIgnoreCase(data, "room");
        var players = GetRequiredPropertyIgnoreCase(room, "players")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();

        Assert.That(players, Has.Length.EqualTo(1));
        Assert.That(players[0], Is.EqualTo("alice"));
    }

    [Test]
    public async Task GlobalOperation_PrivateChat_ReturnsGameEndpoint_AndRoomHasBothPlayers()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/global/chat"), cancellationToken);
        _ = await ReceiveTextAsync(socket, cancellationToken); // bootstrap

        await SendJsonAsync(socket, new
        {
            operation = "privatechat",
            payload = new
            {
                playerId = "alice",
                targetPlayerId = "bob",
                roomId = "private-1"
            }
        }, cancellationToken);

        var responsePayload = await ReceiveTextAsync(socket, cancellationToken);
        using var doc = JsonDocument.Parse(responsePayload);
        Assert.That(GetRequiredPropertyIgnoreCase(doc.RootElement, "operation").GetString(), Is.EqualTo("privatechat"));

        var data = GetRequiredPropertyIgnoreCase(doc.RootElement, "data");
        Assert.That(GetRequiredPropertyIgnoreCase(data, "gameEndpoint").GetString(), Is.EqualTo("/ws/games/private-1"));

        var players = GetRequiredPropertyIgnoreCase(GetRequiredPropertyIgnoreCase(data, "room"), "players")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();

        Assert.That(players, Contains.Item("alice"));
        Assert.That(players, Contains.Item("bob"));
    }

    [Test]
    public async Task GameOperation_Command_WhenPlayerNotJoined_ReturnsCommandRejected()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/nojoin-command"), cancellationToken);

        await SendJsonAsync(socket, new
        {
            operation = "command",
            payload = new
            {
                command = new
                {
                    type = "Sync",
                    playerId = "intruder",
                    payload = new
                    {
                        state = new
                        {
                            x = 1
                        }
                    }
                }
            }
        }, cancellationToken);

        var message = await ReceiveByTypeAsync(socket, "CommandRejected", cancellationToken);
        Assert.That(GetRequiredPropertyIgnoreCase(message, "Error").GetString(), Does.Contain("not joined"));
    }

    [Test]
    public async Task GameOperation_Chat_WhenPlayerNotJoined_ReturnsChatRejected()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/nojoin-chat"), cancellationToken);

        await SendJsonAsync(socket, new
        {
            operation = "chat",
            payload = new
            {
                playerId = "intruder",
                text = "hello"
            }
        }, cancellationToken);

        var message = await ReceiveByTypeAsync(socket, "ChatRejected", cancellationToken);
        Assert.That(GetRequiredPropertyIgnoreCase(message, "Error").GetString(), Does.Contain("not joined"));
    }

    [Test]
    public async Task GameOperation_UnsupportedOperation_ReturnsOperationRejected()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/unsupported-op"), cancellationToken);

        await SendJsonAsync(socket, new
        {
            operation = "dance",
            payload = new { }
        }, cancellationToken);

        var payload = await ReceiveTextAsync(socket, cancellationToken);
        using var doc = JsonDocument.Parse(payload);
        Assert.That(GetRequiredPropertyIgnoreCase(doc.RootElement, "type").GetString(), Is.EqualTo("OperationRejected"));
        Assert.That(GetRequiredPropertyIgnoreCase(doc.RootElement, "error").GetString(), Does.Contain("Unsupported operation"));
    }

    private static async Task SendJsonAsync(WebSocket socket, object payload, CancellationToken cancellationToken)
    {
        var text = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(text);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<string> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return string.Empty;
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

    private static async Task<JsonElement> ReceiveByTypeAsync(WebSocket socket, string expectedType, CancellationToken cancellationToken)
    {
        while (true)
        {
            var payload = await ReceiveTextAsync(socket, cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(payload);
            if (!TryGetPropertyIgnoreCase(doc.RootElement, "type", out var typeElement))
            {
                continue;
            }

            if (string.Equals(typeElement.GetString(), expectedType, StringComparison.Ordinal))
            {
                return doc.RootElement.Clone();
            }
        }
    }

    private static JsonElement GetRequiredPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"Missing property '{propertyName}'.");
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}

