using Haggis.Domain.Model;
using System.Collections.Generic;

namespace MonteCarlo
{
    public interface IMonteCarloActionSelectionStrategy
    {
        IList<MonteCarloHaggisAction> Select(RoundState state, IList<MonteCarloHaggisAction> generatedActions);
    }
}
