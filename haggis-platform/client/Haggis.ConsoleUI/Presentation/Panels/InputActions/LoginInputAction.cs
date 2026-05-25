namespace Haggis.ConsoleUI.Presentation.Panels.InputActions;

public abstract record LoginInputAction : IInputAction
{
    public sealed record Submit(string Value) : LoginInputAction;
}
