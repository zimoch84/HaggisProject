namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketChatOperationDto : GameWebSocketOperationDto
{
    public GameWebSocketChatOperationDto() : base(GameWebSocketOperationType.Chat)
    {
    }

    [System.Text.Json.Serialization.JsonRequired]
    public GameWebSocketChatPayloadDto? Payload { get; init; }
}
