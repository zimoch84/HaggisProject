public sealed class RemoteCreateGamePlayerDto
{
    public string Id { get; init; } = string.Empty;

    public string? Type { get; init; }

    public RemoteCreateGameAiOptionsDto? Ai { get; init; }
}
