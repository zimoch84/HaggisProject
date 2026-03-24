using System.Text.Json.Serialization;

internal sealed class RemoteTrickActionDto
{
    public string? PlayerId { get; init; }
    public bool? IsPass { get; init; }

    [JsonPropertyName("desc")]
    public string? Description { get; init; }
}
