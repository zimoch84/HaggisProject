public sealed class RemoteAppliedMove
{
    public string PlayerId { get; init; } = string.Empty;
    public bool IsPass { get; init; }
    public string Action { get; init; } = string.Empty;
}
