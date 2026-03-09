using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

internal sealed class SnapshotOperationStrategy : IGameOperationStrategy<GameWebSocketSnapshotOperationDto>
{
    private readonly GameWebSocketHandler _handler;

    public SnapshotOperationStrategy(GameWebSocketHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketSnapshotOperationDto operation, CancellationToken cancellationToken)
    {
        var payloadDto = operation.Payload!;
        var playerId = payloadDto.PlayerId.Trim();

        if (!_handler.IsPlayerAllowedForGame(context.GameId, playerId))
        {
            var rejected = new GameEventMessage(
                Type: "SnapshotRejected",
                OrderPointer: null,
                GameId: context.GameId,
                Error: "Player is not joined to this room.",
                Command: null,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow,
                MessageKind: "response");
            await GameWebSocketHandler.SendToClientAsync(context.Socket, "snapshot", rejected, cancellationToken);
            return;
        }

        _handler.ConnectionManager.BindPlayer(context.GameId, context.ClientId, playerId);
        var snapshot = _handler.ApplicationService.GetSnapshot(context.GameId);
        await GameWebSocketHandler.SendToClientAsync(context.Socket, "snapshot", snapshot, cancellationToken);
    }
}
