using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

internal sealed class CommandOperationStrategy : IGameOperationStrategy
{
    private readonly GameWebSocketHandler _handler;

    public CommandOperationStrategy(GameWebSocketHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketOperationDto operation, CancellationToken cancellationToken)
    {
        if (!GameWebSocketHandler.TryParseCommandMessage(operation, out var commandMessage))
        {
            await GameWebSocketHandler.SendOperationErrorAsync(context.Socket, "command", context.GameId, "Invalid command payload.", cancellationToken);
            return;
        }

        if (!_handler.IsPlayerAllowedForGame(context.GameId, commandMessage.Command.PlayerId))
        {
            var rejected = new GameEventMessage(
                Type: "CommandRejected",
                OrderPointer: null,
                GameId: context.GameId,
                Error: "Player is not joined to this room.",
                Command: commandMessage.Command,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow);
            await GameWebSocketHandler.SendToClientAsync(context.Socket, "command", rejected, cancellationToken);
            return;
        }

        _handler.ConnectionManager.BindPlayer(context.GameId, context.ClientId, commandMessage.Command.PlayerId);

        var outgoing = _handler.ApplicationService.Handle(context.GameId, commandMessage);
        if (!outgoing.Type.Equals("CommandApplied", StringComparison.Ordinal))
        {
            await GameWebSocketHandler.SendToClientAsync(context.Socket, "command", outgoing, cancellationToken);
            return;
        }

        await _handler.BroadcastAsync(context.GameId, "command", outgoing, cancellationToken);
    }
}
