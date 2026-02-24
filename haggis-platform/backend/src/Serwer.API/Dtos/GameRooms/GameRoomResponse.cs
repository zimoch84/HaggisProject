using System.Text.Json.Serialization;

namespace Serwer.API.Dtos.GameRooms;

public sealed record GameRoomResponse(
    [property: JsonPropertyName("roomId")] string RoomId,
    [property: JsonPropertyName("gameId")] string GameId,
    [property: JsonPropertyName("gameType")] string GameType,
    [property: JsonPropertyName("roomName")] string RoomName,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("players")] IReadOnlyList<string> Players,
    [property: JsonPropertyName("gameEndpoint")] string GameEndpoint);
