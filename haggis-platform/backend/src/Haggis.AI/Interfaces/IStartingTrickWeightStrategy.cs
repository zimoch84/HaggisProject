using System.Collections.Generic;
using Haggis.Domain.Model;

namespace Haggis.AI.Interfaces
{
    public interface IStartingTrickWeightStrategy
    {
        IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState);
    }
}
