using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

internal sealed class CreateOperationStrategy : IGameOperationStrategy
{
    private readonly GameWebSocketHandler _handler;

    public CreateOperationStrategy(GameWebSocketHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketOperationDto operation, CancellationToken cancellationToken)
    {
        if (!GameWebSocketHandler.TryParseCreatePayload(operation, out var playerId, out var payload))
        {
            await GameWebSocketHandler.SendOperationErrorAsync(context.Socket, "create", context.GameId, "Invalid create payload.", cancellationToken);
            return;
        }

        GameRoom? room;
        if (!_handler.RoomStore.TryJoinRoom(context.GameId, playerId, out room) || room is null)
        {
            room = _handler.RoomStore.GetOrCreateRoom(context.GameId, playerId, "haggis");
        }

        _handler.ConnectionManager.BindPlayer(context.GameId, context.ClientId, playerId);

        var initializeMessage = new GameClientMessage(
            Type: "Command",
            Command: new GameCommand(
                Type: "Initialize",
                PlayerId: playerId,
                Payload: payload),
            State: null);

        var outgoing = _handler.ApplicationService.Handle(context.GameId, initializeMessage);
        if (!outgoing.Type.Equals("CommandApplied", StringComparison.Ordinal))
        {
            await GameWebSocketHandler.SendToClientAsync(context.Socket, "create", outgoing, cancellationToken);
            return;
        }

        await _handler.BroadcastAsync(context.GameId, "create", outgoing, cancellationToken);
    }
}
