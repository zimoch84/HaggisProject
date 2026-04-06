using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

internal sealed class CommandOperationStrategy : IGameOperationStrategy<GameWebSocketCommandOperationDto>
{
    private readonly GameWebSocketHandler _handler;

    public CommandOperationStrategy(GameWebSocketHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketCommandOperationDto operation, CancellationToken cancellationToken)
    {
        var commandDto = operation.Payload!.Command!;
        var commandMessage = new GameClientMessage(
            Type: "Command",
            Command: new GameCommand(
                Type: commandDto.Type.Trim(),
                PlayerId: commandDto.PlayerId.Trim(),
                Payload: (commandDto.Payload ?? new GameWebSocketCommandPayloadDto()).ToGameCommandPayload()),
            State: operation.Payload.State);

        if (!_handler.IsPlayerAllowedForGame(context.GameId, commandMessage.Command.PlayerId))
        {
            var rejected = new GameEventMessage(
                Type: "CommandRejected",
                OrderPointer: null,
                GameId: context.GameId,
                Error: "Player is not joined to this room.",
                Command: commandMessage.Command,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow,
                MessageKind: "response");
            await _handler.SendToClientAsync(context.Socket, "command", context.GameId, rejected, cancellationToken);
            return;
        }

        _handler.ConnectionManager.BindPlayer(context.GameId, context.ClientId, commandMessage.Command.PlayerId);

        var outgoing = _handler.ApplicationService.Handle(context.GameId, commandMessage);
        if (!outgoing.Type.Equals("CommandApplied", StringComparison.Ordinal))
        {
            await _handler.SendToClientAsync(context.Socket, "command", context.GameId, outgoing, cancellationToken);
            return;
        }

        var response = outgoing with { MessageKind = "response" };
        await _handler.SendToClientAsync(context.Socket, "command", context.GameId, response, cancellationToken);

        var eventMessage = outgoing with { MessageKind = "event" };
        await _handler.BroadcastExceptAsync(context.GameId, context.Socket, "command", eventMessage, cancellationToken);
    }
}
