using System.Text.Json.Serialization;

namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal abstract class GameWebSocketOperationDto
{
    protected GameWebSocketOperationDto(GameWebSocketOperationType operationType)
    {
        OperationType = operationType;
    }

    [JsonIgnore]
    public GameWebSocketOperationType OperationType { get; }
}
