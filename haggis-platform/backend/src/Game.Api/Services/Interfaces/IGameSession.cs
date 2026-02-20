using Game.API.Services.Models;

namespace Game.API.Services.Interfaces;

public interface IGameSession
{
    string GameId { get; }

    long OrderPointer { get; }

    GameStateSnapshot CurrentState { get; }

    GameApplyResult Apply(GameClientMessage message);
}
