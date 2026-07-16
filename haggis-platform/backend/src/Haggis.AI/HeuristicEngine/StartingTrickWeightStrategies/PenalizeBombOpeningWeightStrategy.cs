using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PenalizeBombOpeningWeightStrategy : IStartingTrickWeightStrategy
    {
        private const int BasePenalty = 100;

        private float Weight { get; }

        public PenalizeBombOpeningWeightStrategy(float weight)
        {
            Weight = weight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (GetWeightForTrick(trick, gameState), trick))
                .ToList();
        }

        private int GetWeightForTrick(Trick trick, RoundState gameState)
        {
            if (trick == null || gameState?.CurrentPlayer?.Hand == null || Weight <= 0)
            {
                return 0;
            }

            if (trick.Type != TrickType.BOMB)
            {
                return 0;
            }

            var baseScore = HeuristicWeightNormalization.BaseScore(
                -HeuristicWeightNormalization.HandPhase(gameState.CurrentPlayer.Hand.Count),
                BasePenalty);
            return HeuristicWeightNormalization.ApplyWeight(baseScore, Weight);
        }
    }
}
