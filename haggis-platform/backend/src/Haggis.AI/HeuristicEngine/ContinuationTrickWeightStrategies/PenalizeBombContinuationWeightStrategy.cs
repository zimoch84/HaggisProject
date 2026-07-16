using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PenalizeBombContinuationWeightStrategy : IContinuationTrickWeightStrategy
    {
        private const int BasePenalty = 40;

        private float Weight { get; }

        public PenalizeBombContinuationWeightStrategy(float weight)
        {
            Weight = weight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (trick?.Type == TrickType.BOMB
                    ? HeuristicWeightNormalization.ApplyWeight(-BasePenalty, Weight)
                    : 0, trick))
                .ToList();
        }
    }
}
