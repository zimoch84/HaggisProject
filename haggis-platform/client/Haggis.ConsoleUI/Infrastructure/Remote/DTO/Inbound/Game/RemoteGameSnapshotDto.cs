public sealed class RemoteGameSnapshotDto
{
    public long? Version { get; init; }
    public RemoteGameStateDataDto? Data { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
