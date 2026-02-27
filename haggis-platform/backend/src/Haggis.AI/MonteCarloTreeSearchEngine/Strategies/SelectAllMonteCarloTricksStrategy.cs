using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarlo
{
    public sealed class SelectAllMonteCarloTricksStrategy : IMonteCarloTrickSelectionStrategy
    {
        public IList<Trick> Select(RoundState state, IList<Trick> generatedTricks, bool isOpeningTrick)
        {
            return generatedTricks.ToList();
        }
    }
}
