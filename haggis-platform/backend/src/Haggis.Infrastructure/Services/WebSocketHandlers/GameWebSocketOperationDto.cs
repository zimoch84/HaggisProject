using System.Text.Json;

namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketOperationDto
{
    public string RawOperation { get; init; } = string.Empty;
    public GameWebSocketOperationType Operation { get; init; }
    public JsonElement Payload { get; init; }
}
