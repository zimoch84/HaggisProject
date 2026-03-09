public sealed class LobbyState
{
    public List<LobbyRoom> Rooms { get; } = new();
    public List<LobbyChatMessage> Messages { get; } = new();
    public string Status { get; set; } = "Connecting...";
    public string CurrentInputHint { get; set; } = "/create <name>, /join <index|roomId>, /chat <text>, /refresh, /quit";
}

public sealed class LobbyRoom
{
    public string RoomId { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public string GameEndpoint { get; init; } = string.Empty;
    public IReadOnlyList<string> Players { get; init; } = Array.Empty<string>();
}

public sealed class LobbyChatMessage
{
    public string PlayerId { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
}
