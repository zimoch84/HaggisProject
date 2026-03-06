namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketCreateOperationDto : GameWebSocketOperationDto
{
    public GameWebSocketCreateOperationDto() : base(GameWebSocketOperationType.Create)
    {
    }

    [System.Text.Json.Serialization.JsonRequired]
    public GameWebSocketCreatePayloadDto? Payload { get; init; }
}
