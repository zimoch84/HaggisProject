internal sealed class RemotePlayerStateDto
{
    public string? Id { get; init; }
    public int? Score { get; init; }
    public int? HandCount { get; init; }
    public bool? Finished { get; init; }
    public bool? IsAi { get; init; }
    public List<string?>? Hand { get; init; }
}
