using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PreferContinuationsWithFollowUpWeightStrategy : IContinuationTrickWeightStrategy
    {
        private const int BaseWeight = 50;

        private float Weight { get; }

        public PreferContinuationsWithFollowUpWeightStrategy(float weight)
        {
            Weight = weight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            var tricks = allSuggestedTricks ?? new List<Trick>();
            var continuationCounts = tricks
                .Where(trick => trick != null)
                .Select(trick => new
                {
                    Trick = trick,
                    Count = tricks.CountContinuations(trick)
                })
                .ToList();
            var maxContinuationCount = continuationCounts.Any() ? continuationCounts.Max(item => item.Count) : 0;

            return tricks
                .Select(trick =>
                {
                    if (trick == null || Weight <= 0 || maxContinuationCount <= 0)
                    {
                        return (0, trick);
                    }

                    var continuationCount = continuationCounts
                        .Where(item => ReferenceEquals(item.Trick, trick))
                        .Select(item => item.Count)
                        .FirstOrDefault();

                    var baseScore = HeuristicWeightNormalization.BaseScore(
                        HeuristicWeightNormalization.NormalizeRatio(continuationCount, maxContinuationCount),
                        BaseWeight);
                    return (HeuristicWeightNormalization.ApplyWeight(baseScore, Weight), trick);
                })
                .ToList();
        }
    }
}
