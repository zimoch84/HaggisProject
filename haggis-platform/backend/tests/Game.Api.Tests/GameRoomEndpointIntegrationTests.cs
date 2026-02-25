using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Haggis.Infrastructure.Dtos.Chat;
using Haggis.Infrastructure.Dtos.GameRooms;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class GameRoomEndpointIntegrationTests
{
    [Test]
    public async Task CreateGameRoom_WhenRequestValid_ReturnsCreatedRoom()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/rooms/create"), CancellationToken.None);

        await SendTextAsync(socket, "{\"gameType\":\"haggis\",\"hostPlayerId\":\"alice\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        var room = JsonSerializer.Deserialize<GameRoomResponse>(payload);
        Assert.That(room, Is.Not.Null);
        Assert.That(room!.RoomId, Is.Not.Null.And.Not.Empty);
        Assert.That(room.GameId, Is.Not.Null.And.Not.Empty);
        Assert.That(room.GameType, Is.EqualTo("haggis"));
        Assert.That(room.RoomName, Is.EqualTo("alice Haggis game"));
        Assert.That(room.Players[0], Is.EqualTo("alice"));
        Assert.That(room.GameEndpoint, Is.EqualTo($"/games/{room.GameId}/actions"));
    }

    [Test]
    public async Task GameRoomsList_ReturnsCreatedRooms()
    {
        await using var factory = new WebApplicationFactory<Program>();

        await CreateRoomViaWebSocketAsync(factory, "{\"gameType\":\"haggis\",\"hostPlayerId\":\"alice\"}");
        await CreateRoomViaWebSocketAsync(factory, "{\"gameType\":\"haggis\",\"hostPlayerId\":\"bob\"}");

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/rooms/list"), CancellationToken.None);
        await SendTextAsync(socket, "{}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        var rooms = JsonSerializer.Deserialize<List<GameRoomResponse>>(payload);
        Assert.That(rooms, Is.Not.Null);
        Assert.That(rooms!.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task JoinGameRoom_WhenRoomExists_AddsPlayer()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var createdRoom = await CreateRoomViaWebSocketAsync(factory, "{\"gameType\":\"haggis\",\"hostPlayerId\":\"alice\"}");
        Assert.That(createdRoom, Is.Not.Null);

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(
            new Uri($"ws://localhost/ws/rooms/{createdRoom!.RoomId}/join"),
            CancellationToken.None);

        await SendTextAsync(socket, "{\"playerId\":\"bob\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        var joinedRoom = JsonSerializer.Deserialize<GameRoomResponse>(payload);
        Assert.That(joinedRoom, Is.Not.Null);
        Assert.That(joinedRoom!.Players, Contains.Item("alice"));
        Assert.That(joinedRoom.Players, Contains.Item("bob"));
    }

    [Test]
    public async Task JoinGameRoom_WhenRoomNotFound_ReturnsProblemDetails()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/rooms/missing-room/join"), CancellationToken.None);

        await SendTextAsync(socket, "{\"playerId\":\"bob\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        var problem = JsonSerializer.Deserialize<ProblemDetailsMessage>(payload);
        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task CreateGameRoom_WhenGameTypeUnsupported_ReturnsProblemDetails()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/rooms/create"), CancellationToken.None);

        await SendTextAsync(socket, "{\"gameType\":\"chess\",\"hostPlayerId\":\"alice\"}", CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);

        var problem = JsonSerializer.Deserialize<ProblemDetailsMessage>(payload);
        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task CreateGameRoom_WhenRoomNameProvided_UsesProvidedName()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var room = await CreateRoomViaWebSocketAsync(factory, "{\"gameType\":\"haggis\",\"hostPlayerId\":\"alice\",\"roomName\":\"Turniejowy pokoj\"}");
        Assert.That(room, Is.Not.Null);
        Assert.That(room!.RoomName, Is.EqualTo("Turniejowy pokoj"));
    }

    [Test]
    public async Task CreateRoomsEndpoint_WhenNotWebSocketRequest_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ws/rooms/create");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo("WebSocket connection expected."));
    }

    private static async Task<GameRoomResponse?> CreateRoomViaWebSocketAsync(WebApplicationFactory<Program> factory, string requestJson)
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws/rooms/create"), CancellationToken.None);
        await SendTextAsync(socket, requestJson, CancellationToken.None);
        var payload = await ReceiveTextAsync(socket, CancellationToken.None);
        return JsonSerializer.Deserialize<GameRoomResponse>(payload);
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
