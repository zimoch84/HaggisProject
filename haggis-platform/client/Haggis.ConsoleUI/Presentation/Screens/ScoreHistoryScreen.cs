using Haggis.ConsoleUI.Presentation.Panels;
using Haggis.ConsoleUI.Presentation.Panels.InputActions;
using Haggis.ConsoleUI.Presentation.Panels.Inputs;
using Haggis.ConsoleUI.Presentation.ViewModels.Game;

namespace Haggis.ConsoleUI.Presentation.Screens;

public sealed class ScoreHistoryScreen : PanelScreenBase
{
    private readonly UITextPanel _summaryPanel;
    private readonly UITextPanel _tablePanel;
    private ScoreHistoryState _state = new();

    public ScoreHistoryScreen() : base(CreateInputPanel())
    {
        var width = SafeConsoleWidth();
        var height = SafeConsoleHeight();
        var topHeight = Math.Max(8, (height - 5) / 3);
        var bodyHeight = Math.Max(8, height - topHeight - 5);
        var bodyY = 1 + topHeight;

        _summaryPanel = new UITextPanel("Score History", 0, 1, width, topHeight);
        _tablePanel = new UITextPanel("Rounds", 0, bodyY, width, bodyHeight);

        AddPanel(_summaryPanel);
        AddPanel(_tablePanel);
    }

    public async Task ShowAsync(
        ScoreHistoryState state,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        _state = state;
        InputPanel.Clear();
        InputPanel.SetPrompt("Press Enter to close: ");
        await ReadCommandCoreAsync(
            pumpMessagesAsync,
            cancellationToken,
            new GameInputAction.Submit(string.Empty));
    }

    protected override void PreparePanels()
    {
        _summaryPanel.SetLines(new[]
        {
            $"Game: {_state.GameId}",
            $"Completed rounds: {_state.RoundNumbers.Count}",
            "F1 opens this screen during the game."
        });
        _tablePanel.SetLines(BuildTableLines());
        InputPanel.SetPrompt("Press Enter to close: ");
    }

    private IReadOnlyList<string> BuildTableLines()
    {
        if (_state.Players.Count == 0)
        {
            return new[] { "(no score history yet)" };
        }

        var lines = new List<string>();
        var header = "Player".PadRight(14);
        foreach (var round in _state.RoundNumbers)
        {
            header += $"R{round}".PadLeft(6);
        }
        header += "Total".PadLeft(8);
        lines.Add(header);
        lines.Add(new string('-', Math.Min(header.Length, 110)));

        foreach (var player in _state.Players)
        {
            var line = player.PlayerId.PadRight(14);
            foreach (var points in player.RoundPoints)
            {
                var text = points >= 0 ? $"+{points}" : points.ToString();
                line += text.PadLeft(6);
            }

            line += player.TotalPoints.ToString().PadLeft(8);
            lines.Add(line);
        }

        return lines;
    }

    private static GameInput CreateInputPanel()
    {
        return new GameInput("Input", 0, SafeConsoleHeight() - 5, SafeConsoleWidth(), 5);
    }

    private static int SafeConsoleWidth()
    {
        try
        {
            return Math.Max(40, Console.WindowWidth - 1);
        }
        catch
        {
            return 119;
        }
    }

    private static int SafeConsoleHeight()
    {
        try
        {
            return Math.Max(12, Console.WindowHeight - 1);
        }
        catch
        {
            return 29;
        }
    }
}
