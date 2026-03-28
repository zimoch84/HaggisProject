internal sealed class RemoteSnapshotRequestDto
{
    public string Operation { get; init; } = "snapshot";
    public RemotePlayerPayloadDto Payload { get; init; } = new();
}
