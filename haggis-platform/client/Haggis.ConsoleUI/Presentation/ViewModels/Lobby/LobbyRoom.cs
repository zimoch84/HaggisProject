public sealed class LobbyRoom
{
    public string RoomId { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public string GameEndpoint { get; init; } = string.Empty;
    public IReadOnlyList<string> Players { get; init; } = Array.Empty<string>();
}