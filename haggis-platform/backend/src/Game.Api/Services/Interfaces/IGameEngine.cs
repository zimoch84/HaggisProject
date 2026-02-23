using Game.API.Services.Models;

namespace Game.API.Services.Interfaces;

public interface IGameEngine
{
    GameStateSnapshot CreateInitialState(string gameId);

    GameStateSnapshot SimulateNext(string gameId, GameStateSnapshot state, GameCommand command);
}
