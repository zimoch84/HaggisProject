using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface IRoundEndScoringService
    {
        void Apply(HaggisGameState state);
    }
}
