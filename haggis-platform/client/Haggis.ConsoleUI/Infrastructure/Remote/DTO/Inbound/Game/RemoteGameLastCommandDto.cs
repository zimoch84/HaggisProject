using System.Text.Json;

public sealed class RemoteGameLastCommandDto
{
    public string? Type { get; init; }
    public string? PlayerId { get; init; }
    public JsonElement? Payload { get; init; }
}
