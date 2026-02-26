using Haggis.Domain.Model;
using System.Collections.Generic;

namespace Haggis.Domain.Interfaces
{
    public interface IMonteCarloActionSelectionStrategy
    {
        IList<HaggisAction> Select(HaggisGameState state, IList<HaggisAction> generatedActions);
    }
}
