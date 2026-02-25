using System.Text.Json.Serialization;

namespace Haggis.Infrastructure.Dtos.GameRooms;

public sealed record CreateGameRoomRequest(
    [property: JsonPropertyName("gameType")] string GameType,
    [property: JsonPropertyName("hostPlayerId")] string HostPlayerId,
    [property: JsonPropertyName("roomName")] string? RoomName = null);

