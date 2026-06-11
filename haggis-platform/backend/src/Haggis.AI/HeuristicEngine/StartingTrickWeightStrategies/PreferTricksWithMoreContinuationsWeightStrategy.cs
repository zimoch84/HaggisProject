using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PreferTricksWithMoreContinuationsWeightStrategy : IStartingTrickWeightStrategy
    {
        private int ContinuationCountWeight { get; }

        public PreferTricksWithMoreContinuationsWeightStrategy(int continuationCountWeight)
        {
            ContinuationCountWeight = continuationCountWeight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            if (allSuggestedTricks == null || ContinuationCountWeight == 0)
            {
                return new List<(int Weight, Trick Trick)>();
            }

            return allSuggestedTricks
                .Select(trick => (
                    trick == null || trick.Type == TrickType.SINGLE
                        ? 0
                        : allSuggestedTricks.CountContinuations(trick) * ContinuationCountWeight,
                    trick))
                .ToList();
        }
    }
}
