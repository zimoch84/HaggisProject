using Haggis.Model;

namespace Haggis.Interfaces
{
    public interface IPlayStrategy
    {
        HaggisAction GetPlayingAction(HaggisGameState gameState);
    }
}
