using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

internal sealed class JoinOperationStrategy : IGameOperationStrategy
{
    private readonly GameWebSocketHandler _handler;

    public JoinOperationStrategy(GameWebSocketHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketOperationDto operation, CancellationToken cancellationToken)
    {
        if (!GameWebSocketHandler.TryParseJoinPayload(operation, out var playerId))
        {
            await GameWebSocketHandler.SendOperationErrorAsync(context.Socket, "join", context.GameId, "Invalid join payload.", cancellationToken);
            return;
        }

        GameRoom? joinedRoom;
        if (!_handler.RoomStore.TryJoinRoom(context.GameId, playerId, out joinedRoom) || joinedRoom is null)
        {
            joinedRoom = _handler.RoomStore.GetOrCreateRoom(context.GameId, playerId, "haggis");
        }

        _handler.ConnectionManager.BindPlayer(context.GameId, context.ClientId, playerId);

        await _handler.BroadcastRoomJoinedAsync(context.GameId, playerId, joinedRoom, cancellationToken);
    }
}
