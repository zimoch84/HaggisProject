using System.Collections.Concurrent;
using Haggis.Infrastructure.Services.Interfaces;

namespace Haggis.Infrastructure.Services.Infrastructure.Sessions;

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
