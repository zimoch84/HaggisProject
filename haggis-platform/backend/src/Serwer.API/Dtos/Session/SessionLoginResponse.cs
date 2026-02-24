using System.Text.Json.Serialization;

namespace Serwer.API.Dtos.Session;

public sealed record SessionLoginResponse(
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("playerId")] string PlayerId,
    [property: JsonPropertyName("connectedAt")] DateTimeOffset ConnectedAt,
    [property: JsonPropertyName("status")] string Status);
