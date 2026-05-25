internal sealed class RemoteCreateGameRequestDto
{
    public string Operation { get; init; } = "create";
    public RemoteCreateGameEnvelopeDto Payload { get; init; } = new();
}
