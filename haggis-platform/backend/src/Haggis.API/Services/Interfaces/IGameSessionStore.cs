namespace Haggis.API.Services.Interfaces;

public interface IGameSessionStore
{
    IGameSession GetOrCreate(string gameId);
}
