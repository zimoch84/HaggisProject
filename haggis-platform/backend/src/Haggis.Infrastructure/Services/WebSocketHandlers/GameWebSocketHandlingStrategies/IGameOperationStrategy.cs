namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal interface IGameOperationStrategy
{
    Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketOperationDto operation, CancellationToken cancellationToken);
}
