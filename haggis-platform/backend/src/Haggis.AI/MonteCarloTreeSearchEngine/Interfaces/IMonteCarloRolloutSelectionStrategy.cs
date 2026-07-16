using Haggis.Domain.Model;
using System.Collections.Generic;

namespace MonteCarlo
{
    public interface IMonteCarloRolloutSelectionStrategy
    {
        IList<MonteCarloHaggisAction> Select(RoundState state, IList<MonteCarloHaggisAction> generatedActions);
    }
}
