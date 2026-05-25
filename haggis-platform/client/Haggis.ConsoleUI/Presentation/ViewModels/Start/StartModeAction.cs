using Haggis.ConsoleUI.Presentation.Panels;

namespace Haggis.ConsoleUI.Presentation.ViewModels.Start;

public abstract record StartModeAction : IInputAction
{
    public sealed record SinglePlayer : StartModeAction;

    public sealed record MultiPlayer : StartModeAction;

    public sealed record Quit : StartModeAction;

    public sealed record Unknown(string Command) : StartModeAction;
}
