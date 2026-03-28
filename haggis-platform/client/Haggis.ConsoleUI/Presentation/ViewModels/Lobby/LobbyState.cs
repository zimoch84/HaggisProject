public sealed class LobbyState
{
    public List<LobbyRoom> Rooms { get; } = new();
    public List<LobbyChatMessage> Messages { get; } = new();
    public string Status { get; set; } = "Connecting...";
    public string CurrentInputHint { get; set; } = "/create <name>, /join <index|roomId>, /chat <text>, /refresh, /quit";
}