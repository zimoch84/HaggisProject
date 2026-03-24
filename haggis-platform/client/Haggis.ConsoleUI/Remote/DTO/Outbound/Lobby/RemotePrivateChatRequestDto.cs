internal sealed class RemotePrivateChatRequestDto
{
    public string Operation { get; init; } = "privatechat";
    public RemotePrivateChatPayloadDto Payload { get; init; } = new();
}
