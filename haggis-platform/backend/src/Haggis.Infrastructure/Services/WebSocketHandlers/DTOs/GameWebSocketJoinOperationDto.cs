namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketJoinOperationDto : GameWebSocketOperationDto
{
    public GameWebSocketJoinOperationDto() : base(GameWebSocketOperationType.Join)
    {
    }

    [System.Text.Json.Serialization.JsonRequired]
    public GameWebSocketJoinPayloadDto? Payload { get; init; }
}
