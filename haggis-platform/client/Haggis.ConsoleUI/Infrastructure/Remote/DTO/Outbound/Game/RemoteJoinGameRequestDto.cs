internal sealed class RemoteJoinGameRequestDto
{
    public string Operation { get; init; } = "join";
    public RemotePlayerPayloadDto Payload { get; init; } = new();
}
