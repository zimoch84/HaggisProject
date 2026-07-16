using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class RemoteCreateGamePayloadDto
{
    public int? Seed { get; init; }
    public int? PlayerCount { get; init; }
    public List<RemoteCreateGamePlayerDto>? Players { get; init; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}
