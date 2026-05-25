using Haggis.ConsoleUI.Presentation.Panels;
using Haggis.ConsoleUI.Presentation.Panels.InputActions;
using Haggis.ConsoleUI.Presentation.Panels.Inputs;
using Haggis.ConsoleUI.Presentation.ViewModels.Game;

namespace Haggis.ConsoleUI.Presentation.Screens;

public sealed class RoundOverScreen : PanelScreenBase
{
    private readonly UITextPanel _summaryPanel;
    private readonly UITextPanel _scoresPanel;
    private readonly UITextPanel _haggisPanel;
    private readonly UITextPanel _movesPanel;

    private RoundOverState _state = new();

    public RoundOverScreen() : base(CreateInputPanel())
    {
        var width = SafeConsoleWidth();
        var height = SafeConsoleHeight();
        var leftWidth = width / 2;
        var rightWidth = width - leftWidth + 1;
        var topHeight = Math.Max(8, (height - 5) / 3);
        var bodyHeight = Math.Max(8, height - topHeight - 5);
        var bodyY = 1 + topHeight;
        var topLeftWidth = width * 2 / 5;
        var topMiddleWidth = width * 2 / 5;
        var topRightWidth = width - topLeftWidth - topMiddleWidth + 2;

        _summaryPanel = new UITextPanel("Round Over", 0, 1, topLeftWidth, topHeight);
        _scoresPanel = new UITextPanel("Score Table", topLeftWidth - 1, 1, topMiddleWidth, topHeight);
        _haggisPanel = new UITextPanel("Haggis", topLeftWidth + topMiddleWidth - 2, 1, topRightWidth, topHeight);
        _movesPanel = new UITextPanel("Last Sequence", 0, bodyY, width, bodyHeight);

        AddPanel(_summaryPanel);
        AddPanel(_scoresPanel);
        AddPanel(_haggisPanel);
        AddPanel(_movesPanel);
    }

    public async Task ShowAsync(
        RoundOverState state,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        _state = state;
        InputPanel.Clear();
        InputPanel.SetPrompt("Press Enter to continue: ");
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
            $"Round finished: {_state.RoundNumber}",
            $"Next round: {_state.NextRoundNumber}",
            $"Winner: {(_state.WinnerPlayerId == string.Empty ? "(unknown)" : _state.WinnerPlayerId)}",
            _state.Status
        });

        _scoresPanel.SetLines(BuildScoreLines());
        _haggisPanel.SetLines(BuildHaggisLines());
        _movesPanel.SetLines(BuildSequenceLines());
        InputPanel.SetPrompt("Press Enter to continue: ");
    }

    private IReadOnlyList<string> BuildScoreLines()
    {
        if (_state.Players.Count == 0)
        {
            return new[] { "(no scores)" };
        }

        var lines = new List<string>
        {
            "Player       Wzietki Pozost Haggis Runda Total",
            "------------ ------- ------ ------ ----- -----"
        };

        foreach (var player in _state.Players)
        {
            lines.Add(
                $"{TrimCell(player.PlayerId, 12),-12} {FormatSigned(player.TricksPoints),7} {FormatSigned(player.OpponentsRemainingCardsPoints),6} {FormatSigned(player.HaggisPoints),6} {FormatSigned(player.RoundPoints),5} {player.TotalPoints,5}");
        }

        return lines;
    }

    private IReadOnlyList<string> BuildHaggisLines()
    {
        if (_state.HaggisCards.Count == 0)
        {
            return new[] { "(no haggis cards)" };
        }

        return new[]
        {
            $"Cards: {string.Join(" ", _state.HaggisCards)}",
            $"Points: {_state.Players.FirstOrDefault(player => player.HaggisPoints > 0)?.HaggisPoints ?? 0}"
        };
    }

    private IReadOnlyList<string> BuildSequenceLines()
    {
        if (_state.LastSequenceLines.Count == 0)
        {
            return new[] { "(no moves captured)" };
        }

        return _state.LastSequenceLines;
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

    private static string FormatSigned(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static string TrimCell(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength];
    }
}
