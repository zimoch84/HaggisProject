using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Serwer.API.Tests;

[TestFixture]
public class GameRoomEndpointIntegrationTests
{
    [Test]
    public async Task CreateGameRoom_WhenRequestValid_ReturnsCreatedWithGameEndpoint()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/gamerooms", new
        {
            gameType = "haggis",
            hostPlayerId = "alice"
        });

        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(root.GetProperty("roomId").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(root.GetProperty("gameId").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(root.GetProperty("gameType").GetString(), Is.EqualTo("haggis"));
        Assert.That(root.GetProperty("players")[0].GetString(), Is.EqualTo("alice"));

        var gameId = root.GetProperty("gameId").GetString()!;
        Assert.That(root.GetProperty("gameEndpoint").GetString(), Is.EqualTo($"/games/{gameId}/actions"));
    }

    [Test]
    public async Task GameRoomsList_ReturnsCreatedRooms()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/gamerooms", new { gameType = "haggis", hostPlayerId = "alice" });
        await client.PostAsJsonAsync("/api/gamerooms", new { gameType = "haggis", hostPlayerId = "bob" });

        using var response = await client.GetAsync("/api/gamerooms");
        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);
        var rooms = doc.RootElement;

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(rooms.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(rooms.GetArrayLength(), Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task JoinGameRoom_WhenRoomExists_AddsPlayer()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync("/api/gamerooms", new
        {
            gameType = "haggis",
            hostPlayerId = "alice"
        });
        var createdPayload = await createResponse.Content.ReadAsStringAsync();
        using var createdDoc = JsonDocument.Parse(createdPayload);
        var roomId = createdDoc.RootElement.GetProperty("roomId").GetString();

        using var joinResponse = await client.PostAsJsonAsync($"/api/gamerooms/{roomId}/join", new
        {
            playerId = "bob"
        });

        var joinPayload = await joinResponse.Content.ReadAsStringAsync();
        using var joinDoc = JsonDocument.Parse(joinPayload);
        var players = joinDoc.RootElement.GetProperty("players").EnumerateArray().Select(x => x.GetString()).ToArray();

        Assert.That(joinResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(players, Contains.Item("alice"));
        Assert.That(players, Contains.Item("bob"));
    }

    [Test]
    public async Task JoinGameRoom_WhenRoomNotFound_ReturnsNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/gamerooms/missing-room/join", new
        {
            playerId = "bob"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateGameRoom_WhenGameTypeUnsupported_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/gamerooms", new
        {
            gameType = "chess",
            hostPlayerId = "alice"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
