namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketUnknownOperationDto : GameWebSocketOperationDto
{
    public GameWebSocketUnknownOperationDto(string rawOperation)
        : base(GameWebSocketOperationType.Unknown)
    {
        RawOperation = rawOperation;
    }

    public string Operation => RawOperation;
    public string RawOperation { get; }
}
