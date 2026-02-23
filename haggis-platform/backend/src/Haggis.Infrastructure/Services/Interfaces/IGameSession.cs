using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Interfaces;

public interface IGameSession
{
    string GameId { get; }

    long OrderPointer { get; }

    GameStateSnapshot CurrentState { get; }

    GameApplyResult Apply(GameClientMessage message);
}
