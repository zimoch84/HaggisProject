using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PreferContinuationsWithFollowUpWeightStrategy : IContinuationTrickWeightStrategy
    {
        private int ContinuationFollowUpWeight { get; }

        public PreferContinuationsWithFollowUpWeightStrategy(int continuationFollowUpWeight)
        {
            ContinuationFollowUpWeight = continuationFollowUpWeight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            var tricks = allSuggestedTricks ?? new List<Trick>();
            return tricks
                .Select(trick => (trick == null ? 0 : tricks.CountContinuationsWithWilds(trick) * ContinuationFollowUpWeight, trick))
                .ToList();
        }
    }
}
