using Haggis.Domain.Model;
using System.Collections.Generic;

namespace MonteCarlo
{
    public interface IMonteCarloTrickSelectionStrategy
    {
        IList<Trick> Select(RoundState state, IList<Trick> generatedTricks, bool isOpeningTrick);
    }
}
