internal sealed class RemoteCreateGameEnvelopeDto
{
    public string PlayerId { get; init; } = string.Empty;
    public RemoteCreateGamePayloadDto Payload { get; init; } = new();
}
