using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Application;

public interface IGameCommandApplicationService
{
    GameEventMessage Handle(string gameId, GameClientMessage message);
}
