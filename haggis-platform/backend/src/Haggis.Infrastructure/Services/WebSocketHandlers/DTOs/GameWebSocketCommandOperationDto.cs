namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketCommandOperationDto : GameWebSocketOperationDto
{
    public GameWebSocketCommandOperationDto() : base(GameWebSocketOperationType.Command)
    {
    }

    [System.Text.Json.Serialization.JsonRequired]
    public GameWebSocketCommandEnvelopePayloadDto? Payload { get; init; }
}
