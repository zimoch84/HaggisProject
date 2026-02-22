using Haggis.API.Services.Models;

namespace Haggis.API.Services.Interfaces;

public interface IGameEngine
{
    GameStateSnapshot CreateInitialState(string gameId);

    GameStateSnapshot SimulateNext(string gameId, GameStateSnapshot state, GameCommand command);
}
