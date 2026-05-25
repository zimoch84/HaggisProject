internal sealed class RemotePrivateChatPayloadDto
{
    public string PlayerId { get; init; } = string.Empty;
    public string TargetPlayerId { get; init; } = string.Empty;
    public string? RoomName { get; init; }
    public string? RoomId { get; init; }
}
