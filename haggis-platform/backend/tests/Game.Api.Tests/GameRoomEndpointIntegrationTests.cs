using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class GameRoomEndpointIntegrationTests
{
    [Test]
    public async Task RoomSocket_WhenFirstPlayerJoins_CreatesRoomAndBroadcastsRoomJoined()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/rooms/room-alpha"), CancellationToken.None);

        await SendTextAsync(socket, "{\"type\":\"JoinRoom\",\"playerId\":\"alice\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("type").GetString(), Is.EqualTo("RoomJoined"));
        Assert.That(root.GetProperty("gameId").GetString(), Is.EqualTo("room-alpha"));
        Assert.That(root.GetProperty("playerId").GetString(), Is.EqualTo("alice"));
        Assert.That(root.GetProperty("room").GetProperty("roomId").GetString(), Is.EqualTo("room-alpha"));
    }

    [Test]
    public async Task RoomSocket_WhenSecondPlayerJoins_BroadcastsUpdatedRoster()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutCts.Token;

        var wsClientA = factory.Server.CreateWebSocketClient();
        var wsClientB = factory.Server.CreateWebSocketClient();
        using var socketA = await wsClientA.ConnectAsync(new Uri("ws://localhost/ws/rooms/room-bravo"), cancellationToken);
        using var socketB = await wsClientB.ConnectAsync(new Uri("ws://localhost/ws/rooms/room-bravo"), cancellationToken);

        await SendTextAsync(socketA, "{\"type\":\"JoinRoom\",\"playerId\":\"alice\"}", cancellationToken);
        _ = await ReceiveTextAsync(socketA, cancellationToken);

        await SendTextAsync(socketB, "{\"type\":\"JoinRoom\",\"playerId\":\"bob\"}", cancellationToken);
        var payloadA = await ReceiveRoomJoinedForPlayerAsync(socketA, "bob", cancellationToken);
        var payloadB = await ReceiveRoomJoinedForPlayerAsync(socketB, "bob", cancellationToken);

        using var docA = JsonDocument.Parse(payloadA);
        using var docB = JsonDocument.Parse(payloadB);
        var playersA = docA.RootElement.GetProperty("room").GetProperty("players").EnumerateArray().Select(x => x.GetString()).ToArray();
        var playersB = docB.RootElement.GetProperty("room").GetProperty("players").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.That(playersA, Contains.Item("alice"));
        Assert.That(playersA, Contains.Item("bob"));
        Assert.That(playersB, Contains.Item("alice"));
        Assert.That(playersB, Contains.Item("bob"));
    }

    [Test]
    public async Task RoomSocket_WhenNotWebSocketRequest_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/rooms/room-gamma");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
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

    private static async Task<string> ReceiveRoomJoinedForPlayerAsync(WebSocket socket, string playerId, CancellationToken cancellationToken)
    {
        while (true)
        {
            var payload = await ReceiveTextAsync(socket, cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("type", out var typeElement) ||
                !doc.RootElement.TryGetProperty("playerId", out var playerElement))
            {
                continue;
            }

            if (string.Equals(typeElement.GetString(), "RoomJoined", StringComparison.Ordinal) &&
                string.Equals(playerElement.GetString(), playerId, StringComparison.Ordinal))
            {
                return payload;
            }
        }
    }
}
