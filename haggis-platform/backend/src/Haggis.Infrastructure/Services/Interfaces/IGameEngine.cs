using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Interfaces;

public interface IGameEngine
{
    GameStateSnapshot CreateInitialState(string gameId);

    GameStateSnapshot SimulateNext(string gameId, GameStateSnapshot state, GameCommand command);
}
