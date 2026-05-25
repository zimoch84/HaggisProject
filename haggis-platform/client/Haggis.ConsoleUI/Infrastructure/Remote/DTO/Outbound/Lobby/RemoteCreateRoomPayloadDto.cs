internal sealed class RemoteCreateRoomPayloadDto
{
    public string PlayerId { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public string? RoomId { get; init; }
    public string GameType { get; init; } = "haggis";
}
