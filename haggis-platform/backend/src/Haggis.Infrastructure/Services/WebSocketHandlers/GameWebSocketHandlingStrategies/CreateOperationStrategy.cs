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
            await GameWebSocketHandler.SendToClientAsync(context.Socket, "create", outgoing, cancellationToken);
            return;
        }

        var response = outgoing with { MessageKind = "response" };
        await GameWebSocketHandler.SendToClientAsync(context.Socket, "create", response, cancellationToken);

        var eventMessage = outgoing with { MessageKind = "event" };
        await _handler.BroadcastExceptAsync(context.GameId, context.Socket, "create", eventMessage, cancellationToken);
    }
}
