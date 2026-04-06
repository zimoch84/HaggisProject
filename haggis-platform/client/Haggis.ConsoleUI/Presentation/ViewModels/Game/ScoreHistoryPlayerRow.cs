namespace Haggis.ConsoleUI.Presentation.ViewModels.Game;

public sealed class ScoreHistoryPlayerRow
{
    public string PlayerId { get; set; } = string.Empty;

    public IReadOnlyList<int> RoundPoints { get; set; } = Array.Empty<int>();

    public int TotalPoints { get; set; }
}
