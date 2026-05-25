internal sealed class RemoteLobbyInboundMessageDto
{
    public string? Type { get; init; }
    public string? Error { get; init; }
    public string? Title { get; init; }
    public int? Status { get; init; }
    public string? Detail { get; init; }
    public string? Instance { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public List<RemoteLobbyChatChannelDto>? Channels { get; init; }
    public List<RemoteLobbyChatMessageDto>? History { get; init; }
    public string? MessageId { get; init; }
    public string? PlayerId { get; init; }
    public string? Text { get; init; }
    public string? Operation { get; init; }
    public RemoteLobbyOperationDataDto? Data { get; init; }
}
