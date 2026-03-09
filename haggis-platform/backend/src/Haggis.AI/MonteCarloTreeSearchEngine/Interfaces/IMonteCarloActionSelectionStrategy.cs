using Haggis.Domain.Model;
using System.Collections.Generic;

namespace MonteCarlo
{
    public interface IMonteCarloActionSelectionStrategy
    {
        IList<HaggisAction> Select(RoundState state, IList<HaggisAction> generatedActions);
    }
}
