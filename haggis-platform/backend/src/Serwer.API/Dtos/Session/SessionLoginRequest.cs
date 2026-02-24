using System.Text.Json.Serialization;

namespace Serwer.API.Dtos.Session;

public sealed record SessionLoginRequest(
    [property: JsonPropertyName("playerId")] string PlayerId);
