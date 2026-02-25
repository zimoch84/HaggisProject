using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class GameEndpointIntegrationTests
{
    [Test]
    public async Task GameCreateEndpoint_WhenInitializeCommandSent_ResponseContainsAllContractFields()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/game-create-1"), cancellationToken);
        await JoinRoomAsync(socket, "alice", cancellationToken);

        await SendTextAsync(socket,
            JsonSerializer.Serialize(new
            {
                operation = "command",
                payload = new
                {
                    command = new
                    {
                        type = "Initialize",
                        playerId = "alice",
                        payload = new
                        {
                            players = new[] { "alice", "bob", "carol" },
                            seed = 123
                        }
                    }
                }
            }),
            cancellationToken);

        var payload = await ReceiveTextAsync(socket, cancellationToken);
        Assert.That(payload, Is.Not.Empty);

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.That(root.TryGetProperty("Type", out var type), Is.True);
        Assert.That(root.TryGetProperty("OrderPointer", out var orderPointer), Is.True);
        Assert.That(root.TryGetProperty("GameId", out var gameId), Is.True);
        Assert.That(root.TryGetProperty("Error", out var error), Is.True);
        Assert.That(root.TryGetProperty("Command", out var command), Is.True);
        Assert.That(root.TryGetProperty("State", out var state), Is.True);
        Assert.That(root.TryGetProperty("CreatedAt", out var createdAt), Is.True);

        Assert.That(type.GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(orderPointer.GetInt64(), Is.EqualTo(1));
        Assert.That(gameId.GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(error.ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(createdAt.GetDateTimeOffset(), Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));

        Assert.That(command.TryGetProperty("Type", out var commandType), Is.True);
        Assert.That(command.TryGetProperty("PlayerId", out var playerId), Is.True);
        Assert.That(command.TryGetProperty("Payload", out var commandPayload), Is.True);

        Assert.That(commandType.GetString(), Is.EqualTo("Initialize"));
        Assert.That(playerId.GetString(), Is.EqualTo("alice"));
        Assert.That(commandPayload.TryGetProperty("players", out var players), Is.True);
        Assert.That(commandPayload.TryGetProperty("seed", out var seed), Is.True);
        Assert.That(players.GetArrayLength(), Is.EqualTo(3));
        Assert.That(seed.GetInt32(), Is.EqualTo(123));

        Assert.That(state.TryGetProperty("Version", out var version), Is.True);
        Assert.That(state.TryGetProperty("Data", out var data), Is.True);
        Assert.That(state.TryGetProperty("UpdatedAt", out var updatedAt), Is.True);

        Assert.That(version.GetInt64(), Is.EqualTo(1));
        Assert.That(data.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(updatedAt.GetDateTimeOffset(), Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }

    [Test]
    public async Task GameEndpoint_WhenNotWebSocketRequest_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/games/game-1");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
    }

    [Test]
    public async Task GameEndpoint_WhenCommandSent_BroadcastsAppliedEventToConnectedClients()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await wsClientA.ConnectAsync(new Uri("ws://localhost/ws/games/game-9"), cancellationToken);
        using var socketB = await wsClientB.ConnectAsync(new Uri("ws://localhost/ws/games/game-9"), cancellationToken);
        await JoinRoomAsync(socketA, "alice", cancellationToken);
        await SendTextAsync(socketB, "{\"operation\":\"join\",\"payload\":{\"playerId\":\"bob\"}}", cancellationToken);
        _ = await ReceiveTextAsync(socketA, cancellationToken);
        _ = await ReceiveTextAsync(socketB, cancellationToken);

        // Initialize game first so the next Play command is valid for Haggis engine.
        await SendTextAsync(socketA,
            JsonSerializer.Serialize(new
            {
                operation = "command",
                payload = new
                {
                    command = new
                    {
                        type = "Initialize",
                        playerId = "alice",
                        payload = new
                        {
                            players = new[] { "alice", "bob", "carol" },
                            seed = 123
                        }
                    }
                }
            }),
            cancellationToken);

        var initializeMessageA = await ReceiveGameEventAsync(socketA, "CommandApplied", cancellationToken);
        var initializeMessageB = await ReceiveGameEventAsync(socketB, "CommandApplied", cancellationToken);

        AssertAppliedEvent(initializeMessageA, expectedOrderPointer: 1, expectedGameId: "game-9", expectedVersion: 1, expectedPlayerId: "alice", expectedCommandType: "Initialize");
        AssertAppliedEvent(initializeMessageB, expectedOrderPointer: 1, expectedGameId: "game-9", expectedVersion: 1, expectedPlayerId: "alice", expectedCommandType: "Initialize");

        using var initializedDoc = JsonDocument.Parse(initializeMessageA);
        var initializedStateData = initializedDoc.RootElement.GetProperty("State").GetProperty("Data");
        var currentPlayerId = initializedStateData.GetProperty("currentPlayerId").GetString();
        Assert.That(currentPlayerId, Is.Not.Null.And.Not.Empty);

        var chosenPlay = initializedStateData.GetProperty("possibleActions")
            .EnumerateArray()
            .First(x => x.GetProperty("type").GetString() == "Play")
            .GetProperty("action")
            .GetString();
        Assert.That(chosenPlay, Is.Not.Null.And.Not.Empty);

        await SendTextAsync(socketA,
            JsonSerializer.Serialize(new
            {
                operation = "command",
                payload = new
                {
                    command = new
                    {
                        type = "Play",
                        playerId = currentPlayerId,
                        payload = new
                        {
                            action = chosenPlay
                        }
                    }
                }
            }),
            cancellationToken);

        var messageA = await ReceiveGameEventAsync(socketA, "CommandApplied", cancellationToken);
        var messageB = await ReceiveGameEventAsync(socketB, "CommandApplied", cancellationToken);

        AssertAppliedEvent(messageA, expectedOrderPointer: 2, expectedGameId: "game-9", expectedVersion: 2, expectedPlayerId: currentPlayerId!, expectedCommandType: "Play");
        AssertAppliedEvent(messageB, expectedOrderPointer: 2, expectedGameId: "game-9", expectedVersion: 2, expectedPlayerId: currentPlayerId!, expectedCommandType: "Play");

        await socketA.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cancellationToken);
        await socketB.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cancellationToken);
    }

    [Test]
    public async Task GameEndpoint_OrderPointerAndStateVersionIncreaseForSubsequentCommands()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/game-42"), cancellationToken);
        await JoinRoomAsync(socket, "alice", cancellationToken);

        await SendTextAsync(socket,
            JsonSerializer.Serialize(new
            {
                operation = "command",
                payload = new
                {
                    command = new
                    {
                        type = "Initialize",
                        playerId = "alice",
                        payload = new
                        {
                            players = new[] { "alice", "bob", "carol" },
                            seed = 321
                        }
                    }
                }
            }),
            cancellationToken);

        var first = await ReceiveTextAsync(socket, cancellationToken);
        using var firstDoc = JsonDocument.Parse(first);
        var firstStateData = firstDoc.RootElement.GetProperty("State").GetProperty("Data");
        var currentPlayerId = firstStateData.GetProperty("currentPlayerId").GetString();
        Assert.That(currentPlayerId, Is.Not.Null.And.Not.Empty);

        var chosenAction = firstStateData.GetProperty("possibleActions")
            .EnumerateArray()
            .First();
        var secondCommandType = chosenAction.GetProperty("type").GetString();
        Assert.That(secondCommandType, Is.Not.Null.And.Not.Empty);

        object secondPayload = secondCommandType == "Pass"
            ? new { }
            : new { action = chosenAction.GetProperty("action").GetString() };

        await SendTextAsync(socket,
            JsonSerializer.Serialize(new
            {
                operation = "command",
                payload = new
                {
                    command = new
                    {
                        type = secondCommandType,
                        playerId = currentPlayerId,
                        payload = secondPayload
                    }
                }
            }),
            cancellationToken);

        var second = await ReceiveTextAsync(socket, cancellationToken);

        AssertAppliedEvent(first, expectedOrderPointer: 1, expectedGameId: "game-42", expectedVersion: 1, expectedPlayerId: "alice", expectedCommandType: "Initialize");
        AssertAppliedEvent(second, expectedOrderPointer: 2, expectedGameId: "game-42", expectedVersion: 2, expectedPlayerId: currentPlayerId!, expectedCommandType: secondCommandType!);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cancellationToken);
    }

    [Test]
    public async Task GameEndpoint_WhenPayloadContainsState_UsesProvidedStateAsBaseForSimulation()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/game-state"), CancellationToken.None);
        await JoinRoomAsync(socket, "p3", CancellationToken.None);

        await SendTextAsync(socket,
            "{\"operation\":\"command\",\"payload\":{\"command\":{\"type\":\"Sync\",\"playerId\":\"p3\",\"payload\":{\"state\":{\"round\":2,\"phase\":\"trick\"}}},\"state\":{\"version\":5,\"data\":{\"round\":1},\"updatedAt\":\"2026-01-01T00:00:00Z\"}}}",
            CancellationToken.None);

        var message = await ReceiveTextAsync(socket, CancellationToken.None);

        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;
        var state = root.GetProperty("State");

        Assert.That(root.GetProperty("OrderPointer").GetInt64(), Is.EqualTo(1));
        Assert.That(state.GetProperty("Version").GetInt64(), Is.EqualTo(6));
        Assert.That(state.GetProperty("Data").GetProperty("round").GetInt32(), Is.EqualTo(2));
        Assert.That(state.GetProperty("Data").GetProperty("phase").GetString(), Is.EqualTo("trick"));

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Test]
    public async Task GameEndpoint_HaggisInitializeAndPlay_UsesEngineState()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/game-haggis"), CancellationToken.None);
        await JoinRoomAsync(socket, "alice", CancellationToken.None);

        await SendTextAsync(socket,
            JsonSerializer.Serialize(new
            {
                operation = "command",
                payload = new
                {
                    command = new
                    {
                        type = "Initialize",
                        playerId = "alice",
                        payload = new
                        {
                            players = new[] { "alice", "bob", "carol" },
                            seed = 123
                        }
                    }
                }
            }),
            CancellationToken.None);

        var initializedPayload = await ReceiveTextAsync(socket, CancellationToken.None);
        using var initializedDoc = JsonDocument.Parse(initializedPayload);
        var initializedStateData = initializedDoc.RootElement.GetProperty("State").GetProperty("Data");

        Assert.That(initializedStateData.GetProperty("game").GetString(), Is.EqualTo("haggis"));
        Assert.That(initializedStateData.GetProperty("players").GetArrayLength(), Is.EqualTo(3));

        foreach (var player in initializedStateData.GetProperty("players").EnumerateArray())
        {
            var handCount = player.GetProperty("handCount").GetInt32();
            var hand = player.GetProperty("hand");
            Assert.That(hand.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(hand.GetArrayLength(), Is.EqualTo(handCount));
            Assert.That(handCount, Is.EqualTo(17));
        }

        var currentPlayerId = initializedStateData.GetProperty("currentPlayerId").GetString();
        Assert.That(currentPlayerId, Is.Not.Null.And.Not.Empty);

        var chosenPlay = initializedStateData.GetProperty("possibleActions")
            .EnumerateArray()
            .First(x => x.GetProperty("type").GetString() == "Play")
            .GetProperty("action")
            .GetString();
        Assert.That(chosenPlay, Is.Not.Null.And.Not.Empty);

        await SendTextAsync(socket,
            JsonSerializer.Serialize(new
            {
                operation = "command",
                payload = new
                {
                    command = new
                    {
                        type = "Play",
                        playerId = currentPlayerId,
                        payload = new
                        {
                            action = chosenPlay
                        }
                    }
                }
            }),
            CancellationToken.None);

        var playedPayload = await ReceiveTextAsync(socket, CancellationToken.None);
        using var playedDoc = JsonDocument.Parse(playedPayload);
        var playedRoot = playedDoc.RootElement;

        Assert.That(playedRoot.GetProperty("Type").GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(playedRoot.GetProperty("OrderPointer").GetInt64(), Is.EqualTo(2));
        Assert.That(playedRoot.GetProperty("State").GetProperty("Version").GetInt64(), Is.EqualTo(2));
        Assert.That(playedRoot.GetProperty("State").GetProperty("Data").GetProperty("game").GetString(), Is.EqualTo("haggis"));

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Test]
    public async Task GameEndpoint_WhenChatSent_BroadcastsChatEventToConnectedClients()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await wsClientA.ConnectAsync(new Uri("ws://localhost/ws/games/game-chat"), cancellationToken);
        using var socketB = await wsClientB.ConnectAsync(new Uri("ws://localhost/ws/games/game-chat"), cancellationToken);
        await JoinRoomAsync(socketA, "alice", cancellationToken);
        await SendTextAsync(socketB, "{\"operation\":\"join\",\"payload\":{\"playerId\":\"bob\"}}", cancellationToken);
        _ = await ReceiveTextAsync(socketA, cancellationToken);
        _ = await ReceiveTextAsync(socketB, cancellationToken);

        await SendTextAsync(socketA, "{\"operation\":\"chat\",\"payload\":{\"playerId\":\"alice\",\"text\":\"hej wszystkim\"}}", cancellationToken);

        var selfPayload = await ReceiveGameEventAsync(socketA, "ChatPosted", cancellationToken);
        var peerPayload = await ReceiveGameEventAsync(socketB, "ChatPosted", cancellationToken);

        AssertChatEvent(selfPayload, expectedType: "ChatPosted", expectedGameId: "game-chat", expectedPlayerId: "alice", expectedText: "hej wszystkim");
        AssertChatEvent(peerPayload, expectedType: "ChatPosted", expectedGameId: "game-chat", expectedPlayerId: "alice", expectedText: "hej wszystkim");
    }

    private static async Task SendTextAsync(WebSocket socket, string text, CancellationToken cancellationToken)
    {
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

    private static async Task JoinRoomAsync(WebSocket socket, string playerId, CancellationToken cancellationToken)
    {
        await SendTextAsync(socket, JsonSerializer.Serialize(new
        {
            operation = "join",
            payload = new
            {
                playerId
            }
        }), cancellationToken);

        _ = await ReceiveTextAsync(socket, cancellationToken);
    }

    private static async Task<string> ReceiveGameEventAsync(WebSocket socket, string expectedType, CancellationToken cancellationToken)
    {
        while (true)
        {
            var payload = await ReceiveTextAsync(socket, cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(payload);
            if (!TryGetPropertyIgnoreCase(doc.RootElement, "Type", out var typeElement))
            {
                continue;
            }

            var eventType = typeElement.GetString();
            if (string.Equals(eventType, expectedType, StringComparison.Ordinal))
            {
                return payload;
            }
        }
    }

    private static void AssertChatEvent(string payload, string expectedType, string expectedGameId, string expectedPlayerId, string expectedText)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.That(GetRequiredPropertyIgnoreCase(root, "Type").GetString(), Is.EqualTo(expectedType));
        Assert.That(GetRequiredPropertyIgnoreCase(root, "GameId").GetString(), Is.EqualTo(expectedGameId));

        var chat = GetRequiredPropertyIgnoreCase(root, "Chat");
        Assert.That(GetRequiredPropertyIgnoreCase(chat, "PlayerId").GetString(), Is.EqualTo(expectedPlayerId));
        Assert.That(GetRequiredPropertyIgnoreCase(chat, "Text").GetString(), Is.EqualTo(expectedText));
    }


    private static void AssertAppliedEvent(string payload, long expectedOrderPointer, string expectedGameId, long expectedVersion, string expectedPlayerId, string expectedCommandType)
    {
        Assert.That(payload, Is.Not.Empty);

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.That(GetRequiredPropertyIgnoreCase(root, "Type").GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(GetRequiredPropertyIgnoreCase(root, "OrderPointer").GetInt64(), Is.EqualTo(expectedOrderPointer));
        Assert.That(GetRequiredPropertyIgnoreCase(root, "GameId").GetString(), Is.EqualTo(expectedGameId));
        Assert.That(GetRequiredPropertyIgnoreCase(root, "CreatedAt").GetDateTimeOffset(), Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));

        var command = GetRequiredPropertyIgnoreCase(root, "Command");
        Assert.That(GetRequiredPropertyIgnoreCase(command, "Type").GetString(), Is.EqualTo(expectedCommandType));
        Assert.That(GetRequiredPropertyIgnoreCase(command, "PlayerId").GetString(), Is.EqualTo(expectedPlayerId));

        var state = GetRequiredPropertyIgnoreCase(root, "State");
        Assert.That(GetRequiredPropertyIgnoreCase(state, "Version").GetInt64(), Is.EqualTo(expectedVersion));

        var lastCommand = GetRequiredPropertyIgnoreCase(GetRequiredPropertyIgnoreCase(state, "Data"), "lastCommand");
        Assert.That(GetRequiredPropertyIgnoreCase(lastCommand, "type").GetString(), Is.EqualTo(expectedCommandType));
        Assert.That(GetRequiredPropertyIgnoreCase(lastCommand, "playerId").GetString(), Is.EqualTo(expectedPlayerId));
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

