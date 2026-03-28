public sealed class GameInput : PanelRegionInputBase
{
    public GameInput(string header, int x, int y, int width, int height)
        : base(header, x, y, width, height)
    {
    }

    public override IInputAction ParseCommand(string command) => new GameInputAction.Submit(command);
}
