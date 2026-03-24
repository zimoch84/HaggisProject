internal sealed class RemoteCommandEnvelopeDto
{
    public RemoteOutboundCommandDto Command { get; init; } = new();
    public RemoteGameSnapshotDto? State { get; init; }
}
