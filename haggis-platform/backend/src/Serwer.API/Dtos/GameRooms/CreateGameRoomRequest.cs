using System.Text.Json.Serialization;

namespace Serwer.API.Dtos.GameRooms;

public sealed record CreateGameRoomRequest(
    [property: JsonPropertyName("gameType")] string GameType,
    [property: JsonPropertyName("hostPlayerId")] string HostPlayerId,
    [property: JsonPropertyName("roomName")] string? RoomName = null);
