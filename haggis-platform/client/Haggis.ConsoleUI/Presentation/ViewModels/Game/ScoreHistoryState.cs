namespace Haggis.ConsoleUI.Presentation.ViewModels.Game;

public sealed class ScoreHistoryState
{
    public string GameId { get; set; } = string.Empty;

    public IReadOnlyList<int> RoundNumbers { get; set; } = Array.Empty<int>();

    public IReadOnlyList<ScoreHistoryPlayerRow> Players { get; set; } = Array.Empty<ScoreHistoryPlayerRow>();
}
