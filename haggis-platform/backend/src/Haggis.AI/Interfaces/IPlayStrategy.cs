using Haggis.Domain.Model;

namespace Haggis.AI.Interfaces
{
    public interface IPlayStrategy
    {
        HaggisAction GetPlayingAction(HaggisGameState gameState);
    }
}
