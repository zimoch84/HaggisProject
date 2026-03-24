using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class RemoteGameCommandPayloadDto
{
    public string? Action { get; init; }
    public string? Trick { get; init; }
    public bool? Pass { get; init; }
    public int? Seed { get; init; }
    public int? PlayerCount { get; init; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}
