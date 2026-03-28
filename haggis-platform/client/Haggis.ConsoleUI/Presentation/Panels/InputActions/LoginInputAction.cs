public abstract record LoginInputAction : IInputAction
{
    public sealed record Submit(string Value) : LoginInputAction;
}
