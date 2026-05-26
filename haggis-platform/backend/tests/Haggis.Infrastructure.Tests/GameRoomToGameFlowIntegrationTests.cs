using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class GameRoomToGameFlowIntegrationTests
{
    [Test]
    public async Task PlayerOneCanCreateRoom_PlayerTwoCanJoin_AndPlayerOneCanCreateGameUsingRealtimeSocket()
    {
        await using var roomFactory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = timeoutCts.Token;

        const string roomId = "flow-room-1";

        var wsClientA = roomFactory.Server.CreateWebSocketClient();
        var wsClientB = roomFactory.Server.CreateWebSocketClient();
        using var roomSocketA = await wsClientA.ConnectAsync(new Uri($"ws://localhost/ws/games/{roomId}"), cancellationToken);
        using var roomSocketB = await wsClientB.ConnectAsync(new Uri($"ws://localhost/ws/games/{roomId}"), cancellationToken);

        await SendTextAsync(roomSocketA, "{\"operation\":\"join\",\"payload\":{\"playerId\":\"alice\"}}", cancellationToken);
        _ = await ReceiveTextAsync(roomSocketA, cancellationToken);

        await SendTextAsync(roomSocketB, "{\"operation\":\"join\",\"payload\":{\"playerId\":\"bob\"}}", cancellationToken);
        var roomJoinedPayloadA = await ReceiveTextAsync(roomSocketA, cancellationToken);
        var roomJoinedPayloadB = await ReceiveTextAsync(roomSocketB, cancellationToken);

        using var joinedDocA = JsonDocument.Parse(roomJoinedPayloadA);
        using var joinedDocB = JsonDocument.Parse(roomJoinedPayloadB);
        Assert.That(joinedDocA.RootElement.GetProperty("type").GetString(), Is.EqualTo("RoomJoined"));
        Assert.That(joinedDocB.RootElement.GetProperty("type").GetString(), Is.EqualTo("RoomJoined"));
        Assert.That(joinedDocA.RootElement.GetProperty("room").GetProperty("players").EnumerateArray().Select(x => x.GetString()).ToArray(), Contains.Item("bob"));

        await SendTextAsync(
            roomSocketA,
            JsonSerializer.Serialize(new
            {
                operation = "create",
                payload = new
                {
                    playerId = "alice",
                    payload = new
                    {
                        seed = 123
                    }
                }
            }),
            cancellationToken);

        var eventPayload = await ReceiveTextAsync(roomSocketA, cancellationToken);
        Assert.That(eventPayload, Is.Not.Empty);

        using var eventDoc = JsonDocument.Parse(eventPayload);
        var root = eventDoc.RootElement;
        Assert.That(GetRequiredPropertyIgnoreCase(root, "Type").GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(GetRequiredPropertyIgnoreCase(root, "GameId").GetString(), Is.EqualTo(roomId));
        var command = GetRequiredPropertyIgnoreCase(root, "Command");
        Assert.That(GetRequiredPropertyIgnoreCase(command, "Type").GetString(), Is.EqualTo("Initialize"));
        Assert.That(GetRequiredPropertyIgnoreCase(command, "PlayerId").GetString(), Is.EqualTo("alice"));
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

    private static JsonElement GetRequiredPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }
        }

        throw new KeyNotFoundException($"Missing property '{propertyName}'.");
    }
}

