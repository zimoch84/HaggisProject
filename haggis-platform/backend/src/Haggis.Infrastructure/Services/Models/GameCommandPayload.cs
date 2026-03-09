using System.Text.Json;
using System.Text.Json.Serialization;

namespace Haggis.Infrastructure.Services.Models;

public sealed class GameCommandPayload
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("trick")]
    public string? Trick { get; init; }

    [JsonPropertyName("pass")]
    public bool? Pass { get; init; }

    [JsonPropertyName("seed")]
    public int? Seed { get; init; }

    [JsonPropertyName("playerCount")]
    public int? PlayerCount { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }

    [JsonIgnore]
    public JsonValueKind ValueKind => ToJsonElement().ValueKind;

    public JsonElement ToJsonElement()
    {
        return JsonSerializer.SerializeToElement(this, SerializerOptions);
    }

    public bool TryGetProperty(string propertyName, out JsonElement value)
    {
        var payloadElement = ToJsonElement();
        return payloadElement.TryGetProperty(propertyName, out value);
    }

    public string GetRawText()
    {
        return ToJsonElement().GetRawText();
    }

    public static GameCommandPayload FromJsonElement(JsonElement payloadElement)
    {
        if (payloadElement.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return new GameCommandPayload();
        }

        try
        {
            return JsonSerializer.Deserialize<GameCommandPayload>(payloadElement.GetRawText(), SerializerOptions)
                   ?? new GameCommandPayload();
        }
        catch (JsonException)
        {
            return new GameCommandPayload();
        }
    }

    public static implicit operator GameCommandPayload(JsonElement payloadElement) =>
        FromJsonElement(payloadElement);

    public static implicit operator JsonElement(GameCommandPayload payload) =>
        payload.ToJsonElement();
}
