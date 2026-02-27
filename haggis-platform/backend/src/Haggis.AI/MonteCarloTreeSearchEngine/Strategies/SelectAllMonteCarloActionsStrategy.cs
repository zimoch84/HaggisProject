using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarlo
{
    public sealed class SelectAllMonteCarloActionsStrategy : IMonteCarloActionSelectionStrategy
    {
        public IList<HaggisAction> Select(RoundState state, IList<HaggisAction> generatedActions)
        {
            return generatedActions.ToList();
        }
    }
}
