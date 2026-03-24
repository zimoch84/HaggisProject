internal sealed class RemoteGameCommandDto
{
    public string? Type { get; init; }
    public string? PlayerId { get; init; }
    public RemoteGameCommandPayloadDto? Payload { get; init; }
}
