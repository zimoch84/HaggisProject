using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PenalizeWildCardsInContinuationWeightStrategy : IContinuationTrickWeightStrategy
    {
        private const int BasePenalty = 50;

        private float Weight { get; }

        public PenalizeWildCardsInContinuationWeightStrategy(float weight)
        {
            Weight = weight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            var handCount = gameState?.CurrentPlayer?.Hand?.Count ?? 0;
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick =>
                {
                    var wildCount = trick?.Cards.Count(card => card.IsWild) ?? 0;
                    if (wildCount == 0 || Weight <= 0 || trick == null)
                    {
                        return (0, trick);
                    }

                    var wildSignal = HeuristicWeightNormalization.NormalizeRatio(wildCount, trick.Cards.Count);
                    var handSignal = HeuristicWeightNormalization.HandPhase(handCount);
                    var baseScore = HeuristicWeightNormalization.BaseScore(-(wildSignal * handSignal), BasePenalty);
                    return (HeuristicWeightNormalization.ApplyWeight(baseScore, Weight), trick);
                })
                .ToList();
        }
    }
}
