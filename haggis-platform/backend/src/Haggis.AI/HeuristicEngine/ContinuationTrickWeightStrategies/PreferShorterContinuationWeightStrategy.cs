using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PreferShorterContinuationWeightStrategy : IContinuationTrickWeightStrategy
    {
        private int ShorterContinuationWeight { get; }

        public PreferShorterContinuationWeightStrategy(int shorterContinuationWeight)
        {
            ShorterContinuationWeight = shorterContinuationWeight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (trick == null ? 0 : -trick.Cards.Count * ShorterContinuationWeight, trick))
                .ToList();
        }
    }
}
