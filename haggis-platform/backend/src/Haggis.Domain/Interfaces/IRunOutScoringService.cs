using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface IRunOutScoringService
    {
        void Apply(HaggisGameState state, HaggisAction action);
    }
}
