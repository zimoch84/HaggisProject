using System.Text.Json;

internal sealed class RemoteGameLastCommandDto
{
    public string? Type { get; init; }
    public string? PlayerId { get; init; }
    public JsonElement? Payload { get; init; }
}
