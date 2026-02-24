using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Serwer.API.Dtos.Chat;
using Serwer.API.Dtos.GameRooms;
using Serwer.API.Services;

namespace Serwer.API.Tests;

[TestFixture]
public class RoomChatEndpointIntegrationTests
{
    [Test]
    public async Task RoomChatEndpoint_BeforeGame_HostCanChatInRoom()
    {
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        var roomId = await CreateRoomAsync(factory, "alice");

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
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        var roomId = await CreateRoomAsync(factory, "alice");
        await JoinRoomAsync(factory, roomId, "bob");

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
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        var roomId = await CreateRoomAsync(factory, "alice");
        await JoinRoomAsync(factory, roomId, "bob");

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
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/chat/rooms/room-1");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
    }

    [Test]
    public async Task RoomChatEndpoint_WhenNotWebSocketRequestAndRoomMissing_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/chat/rooms/missing");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
    }

    [Test]
    public async Task RoomChatEndpoint_WhenJoinedPlayerSendsMessage_BroadcastsToRoomClients()
    {
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        var roomId = await CreateRoomAsync(factory, "alice");
        await JoinRoomAsync(factory, roomId, "bob");

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
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        var roomId = await CreateRoomAsync(factory, "alice");

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{roomId}"), CancellationToken.None);

        await SendTextAsync(socket, "{\"playerId\":\"mallory\",\"text\":\"hej\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        var problem = JsonSerializer.Deserialize<ProblemDetailsMessage>(payload);
        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Title, Is.EqualTo("Player is not joined to this room."));
        Assert.That(problem.Status, Is.EqualTo(403));
    }

    private static async Task<string> CreateRoomAsync(WebApplicationFactory<GlobalChatHub> factory, string hostPlayerId)
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/rooms/create"), CancellationToken.None);
        await SendTextAsync(socket, $"{{\"gameType\":\"haggis\",\"hostPlayerId\":\"{hostPlayerId}\"}}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);
        var room = JsonSerializer.Deserialize<GameRoomResponse>(payload);
        Assert.That(room, Is.Not.Null);
        return room!.RoomId;
    }

    private static async Task JoinRoomAsync(WebApplicationFactory<GlobalChatHub> factory, string roomId, string playerId)
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri($"ws://localhost/ws/rooms/{roomId}/join"), CancellationToken.None);
        await SendTextAsync(socket, $"{{\"playerId\":\"{playerId}\"}}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);
        var room = JsonSerializer.Deserialize<GameRoomResponse>(payload);
        Assert.That(room, Is.Not.Null);
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
        var message = JsonSerializer.Deserialize<RoomChatMessage>(payload);
        Assert.That(message, Is.Not.Null);
        Assert.That(message!.RoomId, Is.EqualTo(expectedRoomId));
        Assert.That(message.PlayerId, Is.EqualTo(expectedPlayerId));
        Assert.That(message.Text, Is.EqualTo(expectedText));
        Assert.That(message.MessageId, Is.Not.Null.And.Not.Empty);
        Assert.That(message.CreatedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }
}
