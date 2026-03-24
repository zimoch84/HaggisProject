public sealed class RemoteGameState
{
    public long Version { get; set; }
    public string CurrentPlayerId { get; set; } = string.Empty;
    public bool RoundOver { get; set; }
    public List<RemotePlayerState> Players { get; set; } = new();
    public List<RemoteTrickAction> Trick { get; set; } = new();
    public List<RemotePossibleAction> PossibleActions { get; set; } = new();
    public RemoteAppliedMove? AppliedMove { get; set; }
}
