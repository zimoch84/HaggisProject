using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Application;

public sealed class GameCommandApplicationService : IGameCommandApplicationService
{
    private readonly IGameSessionStore _sessionStore;

    public GameCommandApplicationService(IGameSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    public GameEventMessage Handle(string gameId, GameClientMessage message)
    {
        var session = _sessionStore.GetOrCreate(gameId);
        try
        {
            var applyResult = session.Apply(message);
            return new GameEventMessage(
                Type: "CommandApplied",
                OrderPointer: applyResult.OrderPointer,
                GameId: gameId,
                Error: null,
                Command: message.Command,
                State: applyResult.State,
                CreatedAt: DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return new GameEventMessage(
                Type: "CommandRejected",
                OrderPointer: null,
                GameId: gameId,
                Error: ex.Message,
                Command: message.Command,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow);
        }
    }
}
