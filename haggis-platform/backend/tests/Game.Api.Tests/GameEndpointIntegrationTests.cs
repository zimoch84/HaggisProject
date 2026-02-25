using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
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
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/games/create"), cancellationToken);

        await SendTextAsync(socket,
            JsonSerializer.Serialize(new
            {
                type = "Command",
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

        using var response = await client.GetAsync("/games/game-1/actions");
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

        using var socketA = await wsClientA.ConnectAsync(new Uri("ws://localhost/games/game-9/actions"), cancellationToken);
        using var socketB = await wsClientB.ConnectAsync(new Uri("ws://localhost/games/game-9/actions"), cancellationToken);

        // Initialize game first so the next Play command is valid for Haggis engine.
        await SendTextAsync(socketA,
            JsonSerializer.Serialize(new
            {
                type = "Command",
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
            }),
            cancellationToken);

        var initializeMessageA = await ReceiveTextAsync(socketA, cancellationToken);
        var initializeMessageB = await ReceiveTextAsync(socketB, cancellationToken);

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
                type = "Command",
                command = new
                {
                    type = "Play",
                    playerId = currentPlayerId,
                    payload = new
                    {
                        action = chosenPlay
                    }
                }
            }),
            cancellationToken);

        var messageA = await ReceiveTextAsync(socketA, cancellationToken);
        var messageB = await ReceiveTextAsync(socketB, cancellationToken);

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
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/games/game-42/actions"), cancellationToken);

        await SendTextAsync(socket,
            JsonSerializer.Serialize(new
            {
                type = "Command",
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
                type = "Command",
                command = new
                {
                    type = secondCommandType,
                    playerId = currentPlayerId,
                    payload = secondPayload
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
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/games/game-state/actions"), CancellationToken.None);

        await SendTextAsync(socket,
            "{\"type\":\"Command\",\"command\":{\"type\":\"Sync\",\"playerId\":\"p3\",\"payload\":{\"state\":{\"round\":2,\"phase\":\"trick\"}}},\"state\":{\"version\":5,\"data\":{\"round\":1},\"updatedAt\":\"2026-01-01T00:00:00Z\"}}",
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
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/games/game-haggis/actions"), CancellationToken.None);

        await SendTextAsync(socket,
            JsonSerializer.Serialize(new
            {
                type = "Command",
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
                type = "Command",
                command = new
                {
                    type = "Play",
                    playerId = currentPlayerId,
                    payload = new
                    {
                        action = chosenPlay
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

        using var socketA = await wsClientA.ConnectAsync(new Uri("ws://localhost/games/game-chat/actions"), cancellationToken);
        using var socketB = await wsClientB.ConnectAsync(new Uri("ws://localhost/games/game-chat/actions"), cancellationToken);

        await SendTextAsync(socketA, "{\"type\":\"Chat\",\"chat\":{\"playerId\":\"alice\",\"text\":\"hej wszystkim\"}}", cancellationToken);

        var selfPayload = await ReceiveTextAsync(socketA, cancellationToken);
        var peerPayload = await ReceiveTextAsync(socketB, cancellationToken);

        AssertChatEvent(selfPayload, expectedType: "ChatPosted", expectedGameId: "game-chat", expectedPlayerId: "alice", expectedText: "hej wszystkim");
        AssertChatEvent(peerPayload, expectedType: "ChatPosted", expectedGameId: "game-chat", expectedPlayerId: "alice", expectedText: "hej wszystkim");
    }

    [Test]
    public async Task AdminBroadcast_WhenAuthorized_SendsServerAnnouncementToAllGames()
    {
        const string adminToken = "test-admin-token";
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["GAME_ADMIN_TOKEN"] = adminToken
                    });
                });
            });

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await wsClientA.ConnectAsync(new Uri("ws://localhost/games/game-a/actions"), cancellationToken);
        using var socketB = await wsClientB.ConnectAsync(new Uri("ws://localhost/games/game-b/actions"), cancellationToken);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Token", adminToken);

        using var requestBody = new StringContent("{\"message\":\"Przerwa techniczna za 5 minut.\"}", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/admin/broadcast", requestBody, cancellationToken);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        var payloadA = await ReceiveTextAsync(socketA, cancellationToken);
        var payloadB = await ReceiveTextAsync(socketB, cancellationToken);

        AssertChatEvent(payloadA, expectedType: "ServerAnnouncement", expectedGameId: "game-a", expectedPlayerId: "server", expectedText: "Przerwa techniczna za 5 minut.");
        AssertChatEvent(payloadB, expectedType: "ServerAnnouncement", expectedGameId: "game-b", expectedPlayerId: "server", expectedText: "Przerwa techniczna za 5 minut.");
    }

    [Test]
    public async Task GameCreateEndpoint_WhenAdminTokenConfigured_WithoutTokenReturnsUnauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["GAME_ADMIN_TOKEN"] = "test-admin-token"
                    });
                });
            });

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/games/create");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AdminKick_WhenAuthorized_ClosesPlayerSocket()
    {
        const string adminToken = "test-admin-token";
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["GAME_ADMIN_TOKEN"] = adminToken
                    });
                });
            });

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/chat/global"), cancellationToken);
        await SendTextAsync(socket, "{\"playerId\":\"alice\",\"text\":\"hello\"}", cancellationToken);
        _ = await ReceiveTextAsync(socket, cancellationToken);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Token", adminToken);

        using var requestBody = new StringContent("{\"playerId\":\"alice\"}", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/admin/kick", requestBody, cancellationToken);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var responseDoc = JsonDocument.Parse(responseBody);
        Assert.That(responseDoc.RootElement.GetProperty("kickedConnections").GetInt32(), Is.GreaterThanOrEqualTo(1));
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

    private static void AssertChatEvent(string payload, string expectedType, string expectedGameId, string expectedPlayerId, string expectedText)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.That(root.GetProperty("Type").GetString(), Is.EqualTo(expectedType));
        Assert.That(root.GetProperty("GameId").GetString(), Is.EqualTo(expectedGameId));

        var chat = root.GetProperty("Chat");
        Assert.That(chat.GetProperty("PlayerId").GetString(), Is.EqualTo(expectedPlayerId));
        Assert.That(chat.GetProperty("Text").GetString(), Is.EqualTo(expectedText));
    }


    private static void AssertAppliedEvent(string payload, long expectedOrderPointer, string expectedGameId, long expectedVersion, string expectedPlayerId, string expectedCommandType)
    {
        Assert.That(payload, Is.Not.Empty);

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.That(root.GetProperty("Type").GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(root.GetProperty("OrderPointer").GetInt64(), Is.EqualTo(expectedOrderPointer));
        Assert.That(root.GetProperty("GameId").GetString(), Is.EqualTo(expectedGameId));
        Assert.That(root.GetProperty("CreatedAt").GetDateTimeOffset(), Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));

        var command = root.GetProperty("Command");
        Assert.That(command.GetProperty("Type").GetString(), Is.EqualTo(expectedCommandType));
        Assert.That(command.GetProperty("PlayerId").GetString(), Is.EqualTo(expectedPlayerId));

        var state = root.GetProperty("State");
        Assert.That(state.GetProperty("Version").GetInt64(), Is.EqualTo(expectedVersion));

        var lastCommand = state.GetProperty("Data").GetProperty("lastCommand");
        Assert.That(lastCommand.GetProperty("type").GetString(), Is.EqualTo(expectedCommandType));
        Assert.That(lastCommand.GetProperty("playerId").GetString(), Is.EqualTo(expectedPlayerId));
    }
}
