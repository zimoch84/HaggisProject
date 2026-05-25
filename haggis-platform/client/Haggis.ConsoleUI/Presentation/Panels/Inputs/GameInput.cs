using Haggis.ConsoleUI.Presentation.Panels.InputActions;

namespace Haggis.ConsoleUI.Presentation.Panels.Inputs;

public sealed class GameInput : PanelRegionInputBase
{
    public GameInput(string header, int x, int y, int width, int height)
        : base(header, x, y, width, height)
    {
    }

    public override IInputAction ParseCommand(string command)
    {
        return command switch
        {
            FunctionKeyF1Token => new GameInputAction.ShowScoreHistory(),
            FunctionKeyF2Token => new GameInputAction.ShowLastRoundSummary(),
            _ => new GameInputAction.Submit(command)
        };
    }
}
