internal sealed class RemoteCommandRequestDto
{
    public string Operation { get; init; } = "command";
    public RemoteCommandEnvelopeDto Payload { get; init; } = new();
}
