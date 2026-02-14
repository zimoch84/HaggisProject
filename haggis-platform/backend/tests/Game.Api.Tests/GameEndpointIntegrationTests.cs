using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Game.API.Tests;

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
    public async Task GameEndpoint_WhenActionSent_BroadcastsEventToConnectedClients()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await wsClientA.ConnectAsync(new Uri("ws://localhost/ws/games/game-9"), CancellationToken.None);
        using var socketB = await wsClientB.ConnectAsync(new Uri("ws://localhost/ws/games/game-9"), CancellationToken.None);

        await SendTextAsync(socketA, "{\"type\":\"Play\",\"playerId\":\"alice\"}", CancellationToken.None);

        var messageA = await ReceiveTextAsync(socketA, CancellationToken.None);
        var messageB = await ReceiveTextAsync(socketB, CancellationToken.None);

        AssertEvent(messageA, expectedOrderPointer: 1, expectedGameId: "game-9");
        AssertEvent(messageB, expectedOrderPointer: 1, expectedGameId: "game-9");

        await socketA.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await socketB.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Test]
    public async Task GameEndpoint_OrderPointerIncreasesForSubsequentActions()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/games/game-42"), CancellationToken.None);

        await SendTextAsync(socket, "{\"type\":\"Play\",\"playerId\":\"p1\"}", CancellationToken.None);
        await SendTextAsync(socket, "{\"type\":\"Pass\",\"playerId\":\"p2\"}", CancellationToken.None);

        var first = await ReceiveTextAsync(socket, CancellationToken.None);
        var second = await ReceiveTextAsync(socket, CancellationToken.None);

        AssertEvent(first, expectedOrderPointer: 1, expectedGameId: "game-42");
        AssertEvent(second, expectedOrderPointer: 2, expectedGameId: "game-42");

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

    private static void AssertEvent(string payload, long expectedOrderPointer, string expectedGameId)
    {
        Assert.That(payload, Is.Not.Empty);

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.That(root.GetProperty("Type").GetString(), Is.EqualTo("PlayerAction"));
        Assert.That(root.GetProperty("OrderPointer").GetInt64(), Is.EqualTo(expectedOrderPointer));
        Assert.That(root.GetProperty("GameId").GetString(), Is.EqualTo(expectedGameId));
        Assert.That(root.GetProperty("Payload").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(root.GetProperty("CreatedAt").GetDateTimeOffset(), Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }
}
