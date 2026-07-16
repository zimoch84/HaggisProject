using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PreferTricksWithMoreContinuationsWeightStrategy : IStartingTrickWeightStrategy
    {
        private const int BaseWeight = 20;

        private float Weight { get; }

        public PreferTricksWithMoreContinuationsWeightStrategy(float weight)
        {
            Weight = weight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            if (allSuggestedTricks == null || Weight <= 0)
            {
                return new List<(int Weight, Trick Trick)>();
            }

            var comparableTricks = allSuggestedTricks
                .Where(trick => trick != null && trick.Type != TrickType.SINGLE)
                .Select(trick => new
                {
                    Trick = trick,
                    Continuations = allSuggestedTricks.CountContinuations(trick)
                })
                .ToList();
            var maxContinuationCount = comparableTricks.Any()
                ? comparableTricks.Max(item => item.Continuations)
                : 0;

            return allSuggestedTricks
                .Select(trick =>
                {
                    if (trick == null || trick.Type == TrickType.SINGLE || maxContinuationCount <= 0)
                    {
                        return (0, trick);
                    }

                    var continuationCount = comparableTricks
                        .Where(item => ReferenceEquals(item.Trick, trick))
                        .Select(item => item.Continuations)
                        .FirstOrDefault();

                    return (
                        HeuristicWeightNormalization.ApplyWeight(
                            HeuristicWeightNormalization.BaseScore(
                                HeuristicWeightNormalization.NormalizeRatio(continuationCount, maxContinuationCount),
                                BaseWeight),
                            Weight),
                        trick);
                })
                .ToList();
        }
    }
}
