using Haggis.ConsoleUI.Presentation.Panels.InputActions;

namespace Haggis.ConsoleUI.Presentation.Panels.Inputs;

public sealed class LoginInput : PanelRegionInputBase
{
    public LoginInput(string header, int x, int y, int width, int height)
        : base(header, x, y, width, height)
    {
    }

    public override IInputAction ParseCommand(string command) => new LoginInputAction.Submit(command);
}
