internal sealed class RemoteLobbyChatMessageDto
{
    public string? MessageId { get; init; }
    public string? PlayerId { get; init; }
    public string? Text { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}
