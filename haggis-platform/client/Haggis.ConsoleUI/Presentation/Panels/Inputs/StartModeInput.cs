using Haggis.ConsoleUI.Presentation.ViewModels.Start;

namespace Haggis.ConsoleUI.Presentation.Panels.Inputs;

public sealed class StartModeInput : PanelRegionInputBase
{
    public StartModeInput(string header, int x, int y, int width, int height)
        : base(header, x, y, width, height)
    {
    }

    public override IInputAction ParseCommand(string command)
    {
        var normalized = command.Trim();
        if (normalized.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("single", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("singleplayer", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("single-player", StringComparison.OrdinalIgnoreCase))
        {
            return new StartModeAction.SinglePlayer();
        }

        if (normalized.Equals("2", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("multi", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("multiplayer", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("multi-player", StringComparison.OrdinalIgnoreCase))
        {
            return new StartModeAction.MultiPlayer();
        }

        if (normalized.Equals("/quit", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            return new StartModeAction.Quit();
        }

        return new StartModeAction.Unknown(command);
    }
}
