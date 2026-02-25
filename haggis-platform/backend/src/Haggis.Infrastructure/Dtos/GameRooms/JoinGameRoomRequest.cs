using System.Text.Json.Serialization;

namespace Haggis.Infrastructure.Dtos.GameRooms;

public sealed record JoinGameRoomRequest(
    [property: JsonPropertyName("playerId")] string PlayerId);

