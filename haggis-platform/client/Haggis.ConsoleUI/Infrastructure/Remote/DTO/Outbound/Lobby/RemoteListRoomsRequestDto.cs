internal sealed class RemoteListRoomsRequestDto
{
    public string Operation { get; init; } = "listroom";
    public RemoteEmptyPayloadDto Payload { get; init; } = new();
}
