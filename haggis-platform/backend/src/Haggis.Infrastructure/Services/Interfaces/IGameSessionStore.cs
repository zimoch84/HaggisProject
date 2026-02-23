namespace Haggis.Infrastructure.Services.Interfaces;

public interface IGameSessionStore
{
    IGameSession GetOrCreate(string gameId);
}
