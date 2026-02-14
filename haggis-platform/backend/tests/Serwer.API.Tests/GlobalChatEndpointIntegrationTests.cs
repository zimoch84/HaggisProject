using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Serwer.API.Tests;

[TestFixture]
public class GlobalChatEndpointIntegrationTests
{
    [Test]
    public async Task GlobalChatEndpoint_WhenNotWebSocketRequest_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/chat/global");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
    }

    [Test]
    public async Task GlobalChatEndpoint_WhenMessageSent_BroadcastsToAllConnectedClients()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var socketClientA = factory.Server.CreateWebSocketClient();
        var socketClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await socketClientA.ConnectAsync(new Uri("ws://localhost/ws/chat/global"), CancellationToken.None);
        using var socketB = await socketClientB.ConnectAsync(new Uri("ws://localhost/ws/chat/global"), CancellationToken.None);

        await SendTextAsync(socketA, "{\"playerId\":\"alice\",\"text\":\"  hi all  \"}", CancellationToken.None);

        var payloadA = await ReceiveTextAsync(socketA, CancellationToken.None);
        var payloadB = await ReceiveTextAsync(socketB, CancellationToken.None);

        AssertChatPayload(payloadA);
        AssertChatPayload(payloadB);

        await socketA.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test finished", CancellationToken.None);
        await socketB.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test finished", CancellationToken.None);
    }

    private static async Task SendTextAsync(WebSocket socket, string text, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<string> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var stream = new MemoryStream();

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

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }

    private static void AssertChatPayload(string payload)
    {
        Assert.That(payload, Is.Not.Empty);

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.That(root.GetProperty("PlayerId").GetString(), Is.EqualTo("alice"));
        Assert.That(root.GetProperty("Text").GetString(), Is.EqualTo("hi all"));
        Assert.That(root.GetProperty("MessageId").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(root.GetProperty("CreatedAt").GetDateTimeOffset(), Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }
}


