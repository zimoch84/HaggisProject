internal sealed class RemoteGameInboundMessageDto
{
    public string? Type { get; init; }
    public long? OrderPointer { get; init; }
    public string? GameId { get; init; }
    public string? CurrentPlayerId { get; init; }
    public string? Error { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public string? MessageKind { get; init; }
    public string? PlayerId { get; init; }
    public RemoteGameRoomDto? Room { get; init; }
    public RemoteGameSnapshotDto? State { get; init; }
    public RemoteGameCommandDto? Command { get; init; }
    public RemoteGameChatDto? Chat { get; init; }
}
