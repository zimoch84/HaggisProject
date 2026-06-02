using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarlo
{
    public sealed class SelectAllMonteCarloActionsStrategy : IMonteCarloActionSelectionStrategy
    {
        public IList<MonteCarloHaggisAction> Select(RoundState state, IList<MonteCarloHaggisAction> generatedActions)
        {
            return generatedActions.ToList();
        }
    }
}
