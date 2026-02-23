using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Serwer.API.Tests;

[TestFixture]
public class RoomChatEndpointIntegrationTests
{
    [Test]
    public async Task RoomChatEndpoint_BeforeGame_HostCanChatInRoom()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var roomId = await CreateRoomAsync(client, "alice");

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await wsClientA.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);
        using var socketB = await wsClientB.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);

        await SendTextAsync(socketA, "{\"playerId\":\"alice\",\"text\":\"przed gra\"}", CancellationToken.None);

        var payloadA = await ReceiveTextAsync(socketA, CancellationToken.None);
        var payloadB = await ReceiveTextAsync(socketB, CancellationToken.None);

        AssertRoomChatPayload(payloadA, expectedRoomId: roomId, expectedPlayerId: "alice", expectedText: "przed gra");
        AssertRoomChatPayload(payloadB, expectedRoomId: roomId, expectedPlayerId: "alice", expectedText: "przed gra");
    }

    [Test]
    public async Task RoomChatEndpoint_DuringGame_JoinedPlayersCanChat()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var roomId = await CreateRoomAsync(client, "alice");
        await JoinRoomAsync(client, roomId, "bob");

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await wsClientA.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);
        using var socketB = await wsClientB.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);

        await SendTextAsync(socketB, "{\"playerId\":\"bob\",\"text\":\"w trakcie\"}", CancellationToken.None);

        var payloadA = await ReceiveTextAsync(socketA, CancellationToken.None);
        var payloadB = await ReceiveTextAsync(socketB, CancellationToken.None);

        AssertRoomChatPayload(payloadA, expectedRoomId: roomId, expectedPlayerId: "bob", expectedText: "w trakcie");
        AssertRoomChatPayload(payloadB, expectedRoomId: roomId, expectedPlayerId: "bob", expectedText: "w trakcie");
    }

    [Test]
    public async Task RoomChatEndpoint_AfterGame_ReconnectedPlayersCanStillChat()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var roomId = await CreateRoomAsync(client, "alice");
        await JoinRoomAsync(client, roomId, "bob");

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();

        using (var socketA = await wsClientA.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None))
        using (var socketB = await wsClientB.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None))
        {
            await SendTextAsync(socketA, "{\"playerId\":\"alice\",\"text\":\"koniec rundy\"}", CancellationToken.None);
            _ = await ReceiveTextAsync(socketA, CancellationToken.None);
            _ = await ReceiveTextAsync(socketB, CancellationToken.None);
        }

        using var reconnectA = await wsClientA.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);
        using var reconnectB = await wsClientB.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);

        await SendTextAsync(reconnectB, "{\"playerId\":\"bob\",\"text\":\"po grze\"}", CancellationToken.None);

        var payloadA = await ReceiveTextAsync(reconnectA, CancellationToken.None);
        var payloadB = await ReceiveTextAsync(reconnectB, CancellationToken.None);

        AssertRoomChatPayload(payloadA, expectedRoomId: roomId, expectedPlayerId: "bob", expectedText: "po grze");
        AssertRoomChatPayload(payloadB, expectedRoomId: roomId, expectedPlayerId: "bob", expectedText: "po grze");
    }

    [Test]
    public async Task RoomChatEndpoint_WhenNotWebSocketRequest_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/chat/rooms/room-1");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
    }

    [Test]
    public async Task RoomChatEndpoint_WhenNotWebSocketRequestAndRoomMissing_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/chat/rooms/missing");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
    }

    [Test]
    public async Task RoomChatEndpoint_WhenJoinedPlayerSendsMessage_BroadcastsToRoomClients()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var roomId = await CreateRoomAsync(client, "alice");
        await JoinRoomAsync(client, roomId, "bob");

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await wsClientA.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);
        using var socketB = await wsClientB.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);

        await SendTextAsync(socketA, "{\"playerId\":\"alice\",\"text\":\"  hej pokoj  \"}", CancellationToken.None);

        var payloadA = await ReceiveTextAsync(socketA, CancellationToken.None);
        var payloadB = await ReceiveTextAsync(socketB, CancellationToken.None);

        AssertRoomChatPayload(payloadA, expectedRoomId: roomId, expectedPlayerId: "alice", expectedText: "hej pokoj");
        AssertRoomChatPayload(payloadB, expectedRoomId: roomId, expectedPlayerId: "alice", expectedText: "hej pokoj");
    }

    [Test]
    public async Task RoomChatEndpoint_WhenPlayerIsNotInRoom_ReturnsForbiddenProblem()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var roomId = await CreateRoomAsync(client, "alice");

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);

        await SendTextAsync(socket, "{\"playerId\":\"mallory\",\"text\":\"hej\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("Title").GetString(), Is.EqualTo("Player is not joined to this room."));
        Assert.That(root.GetProperty("Status").GetInt32(), Is.EqualTo(403));
    }

    private static async Task<string> CreateRoomAsync(HttpClient client, string hostPlayerId)
    {
        using var response = await client.PostAsJsonAsync("/api/gamerooms", new
        {
            gameType = "haggis",
            hostPlayerId
        });

        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);
        return doc.RootElement.GetProperty("roomId").GetString()!;
    }

    private static async Task JoinRoomAsync(HttpClient client, string roomId, string playerId)
    {
        using var response = await client.PostAsJsonAsync($"/api/gamerooms/{roomId}/join", new
        {
            playerId
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
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

    private static void AssertRoomChatPayload(string payload, string expectedRoomId, string expectedPlayerId, string expectedText)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.That(root.GetProperty("RoomId").GetString(), Is.EqualTo(expectedRoomId));
        Assert.That(root.GetProperty("PlayerId").GetString(), Is.EqualTo(expectedPlayerId));
        Assert.That(root.GetProperty("Text").GetString(), Is.EqualTo(expectedText));
        Assert.That(root.GetProperty("MessageId").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(root.GetProperty("CreatedAt").GetDateTimeOffset(), Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }
}
