namespace Haggis.ConsoleUI.Presentation.ViewModels.Game;

public sealed class RoundOverState
{
    public string GameId { get; set; } = string.Empty;

    public int RoundNumber { get; set; }

    public int NextRoundNumber { get; set; }

    public string Status { get; set; } = string.Empty;

    public string WinnerPlayerId { get; set; } = string.Empty;

    public IReadOnlyList<RoundOverPlayerSummary> Players { get; set; } = Array.Empty<RoundOverPlayerSummary>();

    public IReadOnlyList<string> HaggisCards { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> LastSequenceLines { get; set; } = Array.Empty<string>();
}
