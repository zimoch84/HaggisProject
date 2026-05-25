namespace Haggis.ConsoleUI.Presentation.Panels.InputActions;

public abstract record GameInputAction : IInputAction
{
    public sealed record Submit(string Value) : GameInputAction;

    public sealed record SelectedAction(RemotePossibleActionDto Action) : GameInputAction;

    public sealed record ShowScoreHistory : GameInputAction;

    public sealed record ShowLastRoundSummary : GameInputAction;
}
