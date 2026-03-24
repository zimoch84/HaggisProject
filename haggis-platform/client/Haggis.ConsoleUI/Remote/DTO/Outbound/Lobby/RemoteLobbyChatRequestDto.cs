internal sealed class RemoteLobbyChatRequestDto
{
    public string Operation { get; init; } = "chat";
    public RemoteLobbyChatPayloadDto Payload { get; init; } = new();
}
