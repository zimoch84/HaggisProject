using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.Infrastructure.Services.Engine;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Serwer.API.Dtos.Chat;
using Serwer.API.Dtos.GameRooms;
using Serwer.API.Dtos.Session;
using Serwer.API.Services;

namespace Serwer.API.Tests;

[TestFixture]
public class SessionEndpointIntegrationTests
{
    [Test]
    public async Task SessionLogin_WhenRequestValid_ReturnsAuthenticatedSession()
    {
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/session/login"), CancellationToken.None);

        await SendTextAsync(socket, "{\"playerId\":\"alice\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        var response = JsonSerializer.Deserialize<SessionLoginResponse>(payload);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.SessionId, Is.Not.Null.And.Not.Empty);
        Assert.That(response.PlayerId, Is.EqualTo("alice"));
        Assert.That(response.Status, Is.EqualTo("authenticated"));
    }

    [Test]
    public async Task SessionLogin_WhenPayloadInvalid_ReturnsProblemDetails()
    {
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/session/login"), CancellationToken.None);

        await SendTextAsync(socket, "{\"playerId\":\"\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        var response = JsonSerializer.Deserialize<ProblemDetailsMessage>(payload);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task SessionLogin_WhenNotWebSocketRequest_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<GlobalChatHub>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/session/login");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
    }

    [Test]
    public async Task EndToEnd_TwoUsersLogin_CreateAndJoinRoom_ChatAndGameEventAreDelivered()
    {
        await using var roomFactory = new WebApplicationFactory<GlobalChatHub>();
        await using var gameFactory = new WebApplicationFactory<HaggisGameEngine>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = timeoutCts.Token;

        var roomSocketClient = roomFactory.Server.CreateWebSocketClient();

        using var aliceLoginSocket = await roomSocketClient.ConnectAsync(new Uri("ws://localhost/ws/session/login"), cancellationToken);
        await SendTextAsync(aliceLoginSocket, "{\"playerId\":\"alice\"}", cancellationToken);
        var aliceLoginPayload = await ReceiveTextAsync(aliceLoginSocket, cancellationToken);
        var aliceLogin = JsonSerializer.Deserialize<SessionLoginResponse>(aliceLoginPayload);

        using var bobLoginSocket = await roomSocketClient.ConnectAsync(new Uri("ws://localhost/ws/session/login"), cancellationToken);
        await SendTextAsync(bobLoginSocket, "{\"playerId\":\"bob\"}", cancellationToken);
        var bobLoginPayload = await ReceiveTextAsync(bobLoginSocket, cancellationToken);
        var bobLogin = JsonSerializer.Deserialize<SessionLoginResponse>(bobLoginPayload);

        Assert.That(aliceLogin, Is.Not.Null);
        Assert.That(aliceLogin!.SessionId, Is.Not.Null.And.Not.Empty);
        Assert.That(aliceLogin.PlayerId, Is.EqualTo("alice"));
        Assert.That(bobLogin, Is.Not.Null);
        Assert.That(bobLogin!.SessionId, Is.Not.Null.And.Not.Empty);
        Assert.That(bobLogin.PlayerId, Is.EqualTo("bob"));
        Assert.That(aliceLogin.SessionId, Is.Not.EqualTo(bobLogin.SessionId));

        using var createRoomSocket = await roomSocketClient.ConnectAsync(new Uri("ws://localhost/ws/rooms/create"), cancellationToken);
        await SendTextAsync(createRoomSocket, "{\"gameType\":\"haggis\",\"hostPlayerId\":\"alice\"}", cancellationToken);
        var createRoomPayload = await ReceiveTextAsync(createRoomSocket, cancellationToken);
        var room = JsonSerializer.Deserialize<GameRoomResponse>(createRoomPayload);

        Assert.That(room, Is.Not.Null);

        using var joinRoomSocket = await roomSocketClient.ConnectAsync(new Uri($"ws://localhost/ws/rooms/{room!.RoomId}/join"), cancellationToken);
        await SendTextAsync(joinRoomSocket, "{\"playerId\":\"bob\"}", cancellationToken);
        var joinRoomPayload = await ReceiveTextAsync(joinRoomSocket, cancellationToken);
        var joinedRoom = JsonSerializer.Deserialize<GameRoomResponse>(joinRoomPayload);

        Assert.That(joinedRoom, Is.Not.Null);
        Assert.That(joinedRoom!.Players, Contains.Item("alice"));
        Assert.That(joinedRoom.Players, Contains.Item("bob"));

        var roomChatClientA = roomFactory.Server.CreateWebSocketClient();
        var roomChatClientB = roomFactory.Server.CreateWebSocketClient();

        using var aliceRoomChatSocket = await roomChatClientA.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{room.RoomId}"), cancellationToken);
        using var bobRoomChatSocket = await roomChatClientB.ConnectAsync(new Uri($"ws://localhost/ws/chat/rooms/{room.RoomId}"), cancellationToken);

        await SendTextAsync(aliceRoomChatSocket, "{\"playerId\":\"alice\",\"text\":\"hej bob\"}", cancellationToken);
        _ = await ReceiveTextAsync(aliceRoomChatSocket, cancellationToken);
        var bobRoomChatPayload = await ReceiveTextAsync(bobRoomChatSocket, cancellationToken);

        var roomChatMessage = JsonSerializer.Deserialize<RoomChatMessage>(bobRoomChatPayload);
        Assert.That(roomChatMessage, Is.Not.Null);
        Assert.That(roomChatMessage!.RoomId, Is.EqualTo(room.RoomId));
        Assert.That(roomChatMessage.PlayerId, Is.EqualTo("alice"));
        Assert.That(roomChatMessage.Text, Is.EqualTo("hej bob"));

        var gameClientA = gameFactory.Server.CreateWebSocketClient();
        var gameClientB = gameFactory.Server.CreateWebSocketClient();

        using var aliceGameSocket = await gameClientA.ConnectAsync(new Uri($"ws://localhost{room.GameEndpoint}"), cancellationToken);
        using var bobGameSocket = await gameClientB.ConnectAsync(new Uri($"ws://localhost{room.GameEndpoint}"), cancellationToken);

        await SendTextAsync(
            aliceGameSocket,
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
                        seed = 77
                    }
                }
            }),
            cancellationToken);

        _ = await ReceiveTextAsync(aliceGameSocket, cancellationToken);
        var bobGameEventPayload = await ReceiveTextAsync(bobGameSocket, cancellationToken);

        using var eventDoc = JsonDocument.Parse(bobGameEventPayload);
        var root = eventDoc.RootElement;
        Assert.That(root.GetProperty("Type").GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(root.GetProperty("GameId").GetString(), Is.EqualTo(room.GameId));
        Assert.That(root.GetProperty("Command").GetProperty("Type").GetString(), Is.EqualTo("Initialize"));
        Assert.That(root.GetProperty("Command").GetProperty("PlayerId").GetString(), Is.EqualTo("alice"));
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
}
