public abstract record GameInputAction : IInputAction
{
    public sealed record Submit(string Value) : GameInputAction;
}
