namespace Haggis.ConsoleUI.Presentation.ViewModels.Game;

public sealed class RoundOverPlayerSummary
{
    public string PlayerId { get; set; } = string.Empty;

    public int TricksPoints { get; set; }

    public int OpponentsRemainingCardsPoints { get; set; }

    public int HaggisPoints { get; set; }

    public int RoundPoints { get; set; }

    public int TotalPoints { get; set; }
}
