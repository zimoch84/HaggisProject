using Haggis.ConsoleUI.Presentation.Panels;
using Haggis.ConsoleUI.Presentation.Panels.Inputs;
using Haggis.ConsoleUI.Presentation.ViewModels.Start;

namespace Haggis.ConsoleUI.Presentation.Screens;

public sealed class StartModeScreen : PanelScreenBase
{
    private readonly UITextPanel _modePanel;
    private readonly UITextPanel _detailsPanel;
    private readonly UITextPanel _commandsPanel;

    private string _playerId = string.Empty;
    private string _status = "Choose game mode.";

    public StartModeScreen() : base(CreateInputPanel())
    {
        var width = SafeConsoleWidth();
        var height = SafeConsoleHeight();
        var leftWidth = width / 2;
        var rightWidth = width - leftWidth;
        var bodyHeight = Math.Max(8, height - 6);

        _modePanel = new UITextPanel("Mode", 0, 1, leftWidth, bodyHeight);
        _detailsPanel = new UITextPanel("Details", leftWidth, 1, rightWidth, bodyHeight / 2);
        _commandsPanel = new UITextPanel("Commands", leftWidth, 1 + bodyHeight / 2, rightWidth, bodyHeight - bodyHeight / 2);

        AddPanel(_modePanel);
        AddPanel(_detailsPanel);
        AddPanel(_commandsPanel);
    }

    public async Task<StartModeAction> ShowAsync(string playerId, CancellationToken cancellationToken)
    {
        _playerId = playerId;
        InputPanel.Clear();

        while (!cancellationToken.IsCancellationRequested)
        {
            StartModeAction cancelAction = new StartModeAction.Quit();
            var action = await ReadCommandCoreAsync(
                () => Task.FromResult(false),
                cancellationToken,
                cancelAction);

            if (action is not StartModeAction.Unknown unknown)
            {
                return action;
            }

            _status = $"Unknown mode '{unknown.Command}'.";
            InputPanel.Clear();
        }

        return new StartModeAction.Quit();
    }

    protected override void PreparePanels()
    {
        _modePanel.SetLines(new[]
        {
            $"User: {_playerId}",
            string.Empty,
            "1. Single Player",
            "2. MultiPlayer",
            string.Empty,
            _status
        });

        _detailsPanel.SetLines(new[]
        {
            "Single Player:",
            "gra z dwoma graczami AI.",
            string.Empty,
            "MultiPlayer:",
            "publiczne lobby, pokoje i chat."
        });

        _commandsPanel.SetLines(new[]
        {
            "Wpisz:",
            "1 albo single",
            "2 albo multi",
            "/quit"
        });

        InputPanel.SetPrompt("Mode: ");
    }

    private static StartModeInput CreateInputPanel()
    {
        return new StartModeInput("Input", 0, SafeConsoleHeight() - 5, SafeConsoleWidth(), 5);
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
