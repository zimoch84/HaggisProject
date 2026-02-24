using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Serwer.API.Dtos.Chat;
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
