internal sealed class RemoteCreateRoomRequestDto
{
    public string Operation { get; init; } = "createroom";
    public RemoteCreateRoomPayloadDto Payload { get; init; } = new();
}
