using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Haggis.Infrastructure.Dtos.GameRooms;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class GameRoomToGameFlowIntegrationTests
{
    [Test]
    public async Task PlayerOneCanCreateRoom_PlayerTwoCanJoin_AndPlayerOneCanInitializeGameUsingRoomGameEndpoint()
    {
        await using var roomFactory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = timeoutCts.Token;

        var roomSocketClient = roomFactory.Server.CreateWebSocketClient();
        using var createRoomSocket = await roomSocketClient.ConnectAsync(new Uri("ws://localhost/ws/rooms/create"), cancellationToken);
        await SendTextAsync(createRoomSocket, "{\"gameType\":\"haggis\",\"hostPlayerId\":\"alice\"}", cancellationToken);
        var createdRoomPayload = await ReceiveTextAsync(createRoomSocket, cancellationToken);
        var createdRoom = JsonSerializer.Deserialize<GameRoomResponse>(createdRoomPayload);
        Assert.That(createdRoom, Is.Not.Null);

        using var joinRoomSocket = await roomSocketClient.ConnectAsync(
            new Uri($"ws://localhost/ws/rooms/{createdRoom!.RoomId}/join"),
            cancellationToken);
        await SendTextAsync(joinRoomSocket, "{\"playerId\":\"bob\"}", cancellationToken);
        var joinedRoomPayload = await ReceiveTextAsync(joinRoomSocket, cancellationToken);
        var joinedRoom = JsonSerializer.Deserialize<GameRoomResponse>(joinedRoomPayload);
        Assert.That(joinedRoom, Is.Not.Null);
        Assert.That(joinedRoom!.Players, Contains.Item("alice"));
        Assert.That(joinedRoom.Players, Contains.Item("bob"));

        var wsClient = roomFactory.Server.CreateWebSocketClient();
        using var gameSocket = await wsClient.ConnectAsync(new Uri($"ws://localhost{createdRoom.GameEndpoint}"), cancellationToken);

        await SendTextAsync(
            gameSocket,
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

        var eventPayload = await ReceiveTextAsync(gameSocket, cancellationToken);
        Assert.That(eventPayload, Is.Not.Empty);

        using var eventDoc = JsonDocument.Parse(eventPayload);
        var root = eventDoc.RootElement;
        Assert.That(root.GetProperty("Type").GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(root.GetProperty("GameId").GetString(), Is.EqualTo(createdRoom.GameId));
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
