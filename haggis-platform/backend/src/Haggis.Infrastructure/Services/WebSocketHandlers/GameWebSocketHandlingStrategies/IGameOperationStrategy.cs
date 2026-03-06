namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal interface IGameOperationStrategy<in TOperation>
    where TOperation : GameWebSocketOperationDto
{
    Task HandleAsync(GameWebSocketHandler.OperationContext context, TOperation operation, CancellationToken cancellationToken);
}
