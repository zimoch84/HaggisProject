internal sealed class RemoteLobbyRoomDto
{
    public string? RoomId { get; init; }
    public string? GameId { get; init; }
    public string? GameType { get; init; }
    public string? RoomName { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public string? GameEndpoint { get; init; }
    public List<string?>? Players { get; init; }
}
