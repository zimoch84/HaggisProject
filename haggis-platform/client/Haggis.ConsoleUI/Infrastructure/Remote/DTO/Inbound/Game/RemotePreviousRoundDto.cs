public sealed class RemotePreviousRoundDto
{
    public int? RoundNumber { get; init; }
    public string? WinnerPlayerName { get; init; }
    public List<string?>? FinishingOrderPlayerNames { get; init; }
    public List<string?>? HaggisCards { get; init; }
    public List<RemotePreviousRoundPlayerScoreDto>? PlayerScores { get; init; }
}
