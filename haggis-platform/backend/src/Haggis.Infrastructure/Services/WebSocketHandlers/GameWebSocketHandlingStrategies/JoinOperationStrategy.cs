using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

internal sealed class JoinOperationStrategy : IGameOperationStrategy<GameWebSocketJoinOperationDto>
{
    private readonly GameWebSocketHandler _handler;

    public JoinOperationStrategy(GameWebSocketHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketJoinOperationDto operation, CancellationToken cancellationToken)
    {
        var payloadDto = operation.Payload!;
        var playerId = payloadDto.PlayerId.Trim();

        GameRoom? joinedRoom;
        if (!_handler.RoomStore.TryJoinRoom(context.GameId, playerId, out joinedRoom) || joinedRoom is null)
        {
            var isSinglePlayerRoom = IsSinglePlayerRoom(context.GameId);
            var roomName = isSinglePlayerRoom
                ? $"Single Player: {playerId} {context.GameId}"
                : null;
            joinedRoom = _handler.RoomStore.GetOrCreateRoom(
                context.GameId,
                playerId,
                "haggis",
                roomName,
                isPublic: !isSinglePlayerRoom);
        }

        _handler.ConnectionManager.BindPlayer(context.GameId, context.ClientId, playerId);

        await _handler.BroadcastRoomJoinedAsync(context.GameId, playerId, joinedRoom, cancellationToken);
    }

    private static bool IsSinglePlayerRoom(string gameId)
    {
        return gameId.Trim().StartsWith("single-", StringComparison.OrdinalIgnoreCase);
    }
}
