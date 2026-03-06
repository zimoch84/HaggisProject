using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers.GameWebSocketHandlingStrategies;

internal sealed class ChatOperationStrategy : IGameOperationStrategy<GameWebSocketChatOperationDto>
{
    private readonly GameWebSocketHandler _handler;

    public ChatOperationStrategy(GameWebSocketHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(GameWebSocketHandler.OperationContext context, GameWebSocketChatOperationDto operation, CancellationToken cancellationToken)
    {
        var payloadDto = operation.Payload!;
        var playerId = payloadDto.PlayerId.Trim();
        var text = payloadDto.Text.Trim();
        var chatMessage = new GameChatClientMessage(
            Type: "Chat",
            Chat: new GameChatMessage(PlayerId: playerId, Text: text));

        if (!_handler.IsPlayerAllowedForGame(context.GameId, chatMessage.Chat.PlayerId))
        {
            var rejected = new GameEventMessage(
                Type: "ChatRejected",
                OrderPointer: null,
                GameId: context.GameId,
                Error: "Player is not joined to this room.",
                Command: null,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow,
                MessageKind: "response");
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
                PlayerId: playerId,
                Text: text),
            MessageKind: "event");

        await _handler.BroadcastAsync(context.GameId, "chat", outgoing, cancellationToken);
    }
}
