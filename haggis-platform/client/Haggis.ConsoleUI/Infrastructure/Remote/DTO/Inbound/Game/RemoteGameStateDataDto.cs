public sealed class RemoteGameStateDataDto
{
    public string? Game { get; init; }
    public int? Seed { get; init; }
    public int? PlayerCount { get; init; }
    public int? WinScore { get; init; }
    public int? RoundNumber { get; init; }
    public long? MoveIteration { get; init; }
    public string? CurrentPlayerId { get; init; }
    public bool? RoundOver { get; init; }
    public bool? GameOver { get; init; }
    public List<RemotePlayerStateDto>? Players { get; init; }
    public List<RemoteTrickActionDto>? Trick { get; init; }
    public List<RemotePossibleActionDto>? PossibleActions { get; init; }
    public RemoteAppliedMoveDto? AppliedMove { get; init; }
    public List<RemoteAppliedMoveDto>? AppliedMoves { get; init; }
    public RemotePreviousRoundDto? PreviousRound { get; init; }
    public RemoteGameLastCommandDto? LastCommand { get; init; }
}
