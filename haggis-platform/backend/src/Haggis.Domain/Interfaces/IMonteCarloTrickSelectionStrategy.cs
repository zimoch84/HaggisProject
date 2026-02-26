using Haggis.Domain.Model;
using System.Collections.Generic;

namespace Haggis.Domain.Interfaces
{
    public interface IMonteCarloTrickSelectionStrategy
    {
        IList<Trick> Select(HaggisGameState state, IList<Trick> generatedTricks, bool isOpeningTrick);
    }
}
