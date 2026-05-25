internal sealed class RemoteOutboundCommandDto
{
    public string Type { get; init; } = string.Empty;
    public string PlayerId { get; init; } = string.Empty;
    public RemoteGameCommandPayloadDto Payload { get; init; } = new();
}
