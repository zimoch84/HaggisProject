using Haggis.ConsoleUI.Presentation.Panels;
using Haggis.ConsoleUI.Presentation.Panels.InputActions;
using Haggis.ConsoleUI.Presentation.Panels.Inputs;

namespace Haggis.ConsoleUI.Presentation.Screens;

public sealed class LoginScreen : PanelScreenBase
{
    private readonly UITextPanel _loginPanel;
    private readonly UITextPanel _infoPanel;
    private readonly UITextPanel _welcomePanel;
    private readonly UITextPanel _tipsPanel;

    public LoginScreen() : base(CreateInputPanel())
    {
        var width = SafeConsoleWidth();
        var height = SafeConsoleHeight();
        var leftWidth = width / 2;
        var rightWidth = width - leftWidth;
        var topHeight = Math.Max(8, (height - 5) / 3);
        var bodyHeight = Math.Max(8, height - topHeight - 5);
        var bodyY = 1 + topHeight;

        _loginPanel = new UITextPanel("Login", 0, 1, leftWidth, topHeight);
        _infoPanel = new UITextPanel("Info", leftWidth, 1, rightWidth, topHeight);
        _welcomePanel = new UITextPanel("Welcome", 0, bodyY, leftWidth, bodyHeight);
        _tipsPanel = new UITextPanel("Tips", leftWidth, bodyY, rightWidth, bodyHeight);

        AddPanel(_loginPanel);
        AddPanel(_infoPanel);
        AddPanel(_welcomePanel);
        AddPanel(_tipsPanel);
    }

    public async Task<string?> ShowAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            InputPanel.Clear();
            var action = await ReadCommandCoreAsync(
                () => Task.FromResult(false),
                cancellationToken,
                new LoginInputAction.Submit(string.Empty));

            if (!string.IsNullOrWhiteSpace(action.Value))
            {
                return action.Value.Trim();
            }
        }

        return null;
    }

    protected override void PreparePanels()
    {
        _loginPanel.SetLines(new[]
        {
            "Podaj nazwe uzytkownika",
            "i nacisnij Enter."
        });
        _infoPanel.SetLines(new[]
        {
            "Tryb zdalny laczy sie z",
            "serwerem WebSocket i",
            "otwiera lobby gier."
        });
        _welcomePanel.SetLines(new[]
        {
            "Po zalogowaniu zobaczysz:",
            "- liste dostepnych gier",
            "- chat publiczny",
            "- opcje tworzenia pokoju",
            "- opcje dolaczania do pokoju"
        });
        _tipsPanel.SetLines(new[]
        {
            "Mozesz pominac ten ekran",
            "uruchamiajac aplikacje z",
            "argumentem:",
            "remote --user=piotr"
        });
        InputPanel.SetPrompt("User: ");
    }

    private static LoginInput CreateInputPanel()
    {
        return new LoginInput("Input", 0, SafeConsoleHeight() - 5, SafeConsoleWidth(), 5);
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
