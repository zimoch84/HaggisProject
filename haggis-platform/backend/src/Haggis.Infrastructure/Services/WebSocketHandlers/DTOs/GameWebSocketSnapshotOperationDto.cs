namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketSnapshotOperationDto : GameWebSocketOperationDto
{
    public GameWebSocketSnapshotOperationDto() : base(GameWebSocketOperationType.Snapshot)
    {
    }

    [System.Text.Json.Serialization.JsonRequired]
    public GameWebSocketSnapshotPayloadDto? Payload { get; init; }
}
