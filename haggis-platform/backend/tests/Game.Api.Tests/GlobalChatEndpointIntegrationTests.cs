using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Haggis.Infrastructure.Services;
using Haggis.Infrastructure.Dtos.Chat;

namespace Haggis.Infrastructure.Tests;

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

    [Test]
    public async Task GlobalChatEndpoint_WhenPayloadInvalid_ReturnsProblemDetails()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var socketClient = factory.Server.CreateWebSocketClient();
        using var socket = await socketClient.ConnectAsync(new Uri("ws://localhost/ws/chat/global"), CancellationToken.None);

        await SendTextAsync(socket, "{\"playerId\":\"\",\"text\":\"hello\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        var problem = JsonSerializer.Deserialize<ProblemDetailsMessage>(payload);
        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Title, Is.EqualTo("Invalid chat payload."));
        Assert.That(problem.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task GlobalChatEndpoint_WhenMultipleMessagesSent_PreservesOrderForReceiver()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var socketClientA = factory.Server.CreateWebSocketClient();
        var socketClientB = factory.Server.CreateWebSocketClient();

        using var socketA = await socketClientA.ConnectAsync(new Uri("ws://localhost/ws/chat/global"), CancellationToken.None);
        using var socketB = await socketClientB.ConnectAsync(new Uri("ws://localhost/ws/chat/global"), CancellationToken.None);

        await SendTextAsync(socketA, "{\"playerId\":\"alice\",\"text\":\"first\"}", CancellationToken.None);
        await SendTextAsync(socketA, "{\"playerId\":\"alice\",\"text\":\"second\"}", CancellationToken.None);

        var firstPayload = await ReceiveTextAsync(socketB, CancellationToken.None);
        var secondPayload = await ReceiveTextAsync(socketB, CancellationToken.None);

        var firstMessage = JsonSerializer.Deserialize<ChatMessage>(firstPayload);
        var secondMessage = JsonSerializer.Deserialize<ChatMessage>(secondPayload);

        Assert.That(firstMessage, Is.Not.Null);
        Assert.That(secondMessage, Is.Not.Null);
        Assert.That(firstMessage!.Text, Is.EqualTo("first"));
        Assert.That(secondMessage!.Text, Is.EqualTo("second"));
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

        var message = JsonSerializer.Deserialize<ChatMessage>(payload);
        Assert.That(message, Is.Not.Null);
        Assert.That(message!.PlayerId, Is.EqualTo("alice"));
        Assert.That(message.Text, Is.EqualTo("hi all"));
        Assert.That(message.MessageId, Is.Not.Null.And.Not.Empty);
        Assert.That(message.CreatedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }
}



