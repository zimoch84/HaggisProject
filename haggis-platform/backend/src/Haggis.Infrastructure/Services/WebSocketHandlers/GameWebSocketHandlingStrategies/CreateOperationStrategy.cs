using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

internal sealed class CreateOperationStrategy : IGameOperationStrategy<GameWebSocketCreateOperationDto>
{
    private readonly GameWebSocketHandler _handler;

    public CreateOperationStrategy(GameWebSocketHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketCreateOperationDto operation, CancellationToken cancellationToken)
    {
        var payloadDto = operation.Payload!;
        var playerId = payloadDto.PlayerId.Trim();

        GameRoom? room;
        if (!_handler.RoomStore.TryJoinRoom(context.GameId, playerId, out room) || room is null)
        {
            room = _handler.RoomStore.GetOrCreateRoom(context.GameId, playerId, "haggis");
        }

        if (room.Players.Count > 0 &&
            !string.Equals(room.Players[0], playerId, StringComparison.OrdinalIgnoreCase))
        {
            await _handler.SendToClientAsync(
                context.Socket,
                "create",
                context.GameId,
                new
                {
                    type = "OperationRejected",
                    messageKind = "response",
                    gameId = context.GameId,
                    error = "Only the host can start the game.",
                    createdAt = DateTimeOffset.UtcNow
                },
                cancellationToken);
            return;
        }

        _handler.ConnectionManager.BindPlayer(context.GameId, context.ClientId, playerId);

        var initializeMessage = new GameClientMessage(
            Type: "Command",
            Command: new GameCommand(
                Type: "Initialize",
                PlayerId: playerId,
                Payload: (payloadDto.Payload ?? new GameWebSocketCreateOptionsDto()).ToGameCommandPayload()),
            State: null);

        var outgoing = _handler.ApplicationService.Handle(context.GameId, initializeMessage);
        if (!outgoing.Type.Equals("CommandApplied", StringComparison.Ordinal))
        {
            await _handler.SendToClientAsync(context.Socket, "create", context.GameId, outgoing, cancellationToken);
            return;
        }

        var response = outgoing with { MessageKind = "response" };
        await _handler.SendToClientAsync(context.Socket, "create", context.GameId, response, cancellationToken);

        var eventMessage = outgoing with { MessageKind = "event" };
        await _handler.BroadcastExceptAsync(context.GameId, context.Socket, "create", eventMessage, cancellationToken);
    }
}
