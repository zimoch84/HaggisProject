using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Haggis.API.Tests;

[TestFixture]
public class GameEndpointIntegrationTests
{
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

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await wsClientA.ConnectAsync(new Uri("ws://localhost/ws/games/game-9"), CancellationToken.None);
        using var socketB = await wsClientB.ConnectAsync(new Uri("ws://localhost/ws/games/game-9"), CancellationToken.None);

        await SendTextAsync(socketA, "{\"type\":\"Command\",\"command\":{\"type\":\"Play\",\"playerId\":\"alice\",\"payload\":{\"card\":\"A\"}}}", CancellationToken.None);

        var messageA = await ReceiveTextAsync(socketA, CancellationToken.None);
        var messageB = await ReceiveTextAsync(socketB, CancellationToken.None);

        AssertAppliedEvent(messageA, expectedOrderPointer: 1, expectedGameId: "game-9", expectedVersion: 1, expectedPlayerId: "alice", expectedCommandType: "Play");
        AssertAppliedEvent(messageB, expectedOrderPointer: 1, expectedGameId: "game-9", expectedVersion: 1, expectedPlayerId: "alice", expectedCommandType: "Play");

        await socketA.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await socketB.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Test]
    public async Task GameEndpoint_OrderPointerAndStateVersionIncreaseForSubsequentCommands()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/game-42"), CancellationToken.None);

        await SendTextAsync(socket, "{\"type\":\"Command\",\"command\":{\"type\":\"Play\",\"playerId\":\"p1\",\"payload\":{\"card\":\"Q\"}}}", CancellationToken.None);
        await SendTextAsync(socket, "{\"type\":\"Command\",\"command\":{\"type\":\"Pass\",\"playerId\":\"p2\",\"payload\":{}}}", CancellationToken.None);

        var first = await ReceiveTextAsync(socket, CancellationToken.None);
        var second = await ReceiveTextAsync(socket, CancellationToken.None);

        AssertAppliedEvent(first, expectedOrderPointer: 1, expectedGameId: "game-42", expectedVersion: 1, expectedPlayerId: "p1", expectedCommandType: "Play");
        AssertAppliedEvent(second, expectedOrderPointer: 2, expectedGameId: "game-42", expectedVersion: 2, expectedPlayerId: "p2", expectedCommandType: "Pass");

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Test]
    public async Task GameEndpoint_WhenPayloadContainsState_UsesProvidedStateAsBaseForSimulation()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/game-state"), CancellationToken.None);

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
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/game-haggis"), CancellationToken.None);

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
