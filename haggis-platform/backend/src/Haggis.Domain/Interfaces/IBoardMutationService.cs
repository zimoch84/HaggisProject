using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface IBoardMutationService
    {
        void Apply(RoundState state, HaggisAction action);
    }
}
