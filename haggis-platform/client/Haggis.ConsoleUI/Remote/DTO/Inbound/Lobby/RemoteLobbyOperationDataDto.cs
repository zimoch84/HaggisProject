internal sealed class RemoteLobbyOperationDataDto
{
    public string? Type { get; init; }
    public List<RemoteLobbyRoomDto>? Rooms { get; init; }
    public RemoteLobbyRoomDto? Room { get; init; }
    public string? GameEndpoint { get; init; }
    public string? Error { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}
