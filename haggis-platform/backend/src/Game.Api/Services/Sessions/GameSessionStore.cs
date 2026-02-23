using System.Collections.Concurrent;
using Game.API.Services.Interfaces;

namespace Game.API.Services.Sessions;

public sealed class GameSessionStore : IGameSessionStore
{
    private readonly IGameEngine _engine;
    private readonly ConcurrentDictionary<string, IGameSession> _sessions = new();

    public GameSessionStore(IGameEngine engine)
    {
        _engine = engine;
    }

    public IGameSession GetOrCreate(string gameId)
    {
        return _sessions.GetOrAdd(gameId, static (id, engine) => new GameSession(id, engine), _engine);
    }
}
