using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Infrastructure.Sessions;

public sealed class GameSession : IGameSession
{
    private readonly object _gate = new();
    private readonly IGameEngine _engine;

    public GameSession(string gameId, IGameEngine engine)
    {
        GameId = gameId;
        _engine = engine;
        CurrentState = _engine.CreateInitialState(gameId);
    }

    public string GameId { get; }

    public long OrderPointer { get; private set; }

    public GameStateSnapshot CurrentState { get; private set; }

    public GameApplyResult Apply(GameClientMessage message)
    {
        lock (_gate)
        {
            var baseState = message.State ?? CurrentState;
            var nextState = _engine.SimulateNext(GameId, baseState, message.Command);

            CurrentState = nextState;
            OrderPointer++;

            return new GameApplyResult(OrderPointer, nextState);
        }
    }
}
