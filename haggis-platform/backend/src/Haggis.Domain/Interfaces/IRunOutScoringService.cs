using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface IRoundScoringService
    {
        void Apply(RoundState state, HaggisAction action);
    }
}
