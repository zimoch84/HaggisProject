using System.Text.Json.Serialization;

namespace Serwer.API.Dtos.GameRooms;

public sealed record JoinGameRoomRequest(
    [property: JsonPropertyName("playerId")] string PlayerId);
