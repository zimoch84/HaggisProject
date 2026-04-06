using System.Text.Json.Serialization;

public sealed class RemotePossibleActionDto
{
    public string? Type { get; init; }

    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("desc")]
    public string? Description { get; init; }

    [JsonIgnore]
    public string DisplayAction => Action ?? Description ?? string.Empty;
}
