using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

internal sealed class ChatOperationStrategy : IGameOperationStrategy
{
    private readonly GameWebSocketHandler _handler;

    public ChatOperationStrategy(GameWebSocketHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketOperationDto operation, CancellationToken cancellationToken)
    {
        if (!GameWebSocketHandler.TryParseChatMessage(operation, out var chatMessage))
        {
            await GameWebSocketHandler.SendOperationErrorAsync(context.Socket, "chat", context.GameId, "Invalid chat payload.", cancellationToken);
            return;
        }

        if (!_handler.IsPlayerAllowedForGame(context.GameId, chatMessage.Chat.PlayerId))
        {
            var rejected = new GameEventMessage(
                Type: "ChatRejected",
                OrderPointer: null,
                GameId: context.GameId,
                Error: "Player is not joined to this room.",
                Command: null,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow);
            await GameWebSocketHandler.SendToClientAsync(context.Socket, "chat", rejected, cancellationToken);
            return;
        }

        _handler.ConnectionManager.BindPlayer(context.GameId, context.ClientId, chatMessage.Chat.PlayerId);

        var outgoing = new GameEventMessage(
            Type: "ChatPosted",
            OrderPointer: null,
            GameId: context.GameId,
            Error: null,
            Command: null,
            State: null,
            CreatedAt: DateTimeOffset.UtcNow,
            Chat: new GameChatMessage(
                PlayerId: chatMessage.Chat.PlayerId.Trim(),
                Text: chatMessage.Chat.Text.Trim()));

        await _handler.BroadcastAsync(context.GameId, "chat", outgoing, cancellationToken);
    }
}
