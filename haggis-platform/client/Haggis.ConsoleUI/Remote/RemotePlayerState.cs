public sealed class RemotePlayerState
{
    public string Id { get; init; } = string.Empty;
    public int Score { get; init; }
    public int HandCount { get; init; }
    public bool Finished { get; init; }
    public List<string> Hand { get; init; } = new();
}