using System.Collections.Generic;
using System;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PreferShorterTricksWhenHandIsLargeWeightStrategy : IStartingTrickWeightStrategy
    {
        private const int DefaultStartCutoff = 8;
        private const int BaseWeight = 24;

        private float Weight { get; }

        public PreferShorterTricksWhenHandIsLargeWeightStrategy(float weight)
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
            if (trick == null || gameState?.CurrentPlayer == null || Weight <= 0)
            {
                return 0;
            }

            var handCardCount = gameState.CurrentPlayer.Hand?.Count ?? 0;
            if (handCardCount <= 0 || trick.Cards.Count == 0)
            {
                return 0;
            }

            var phaseSignal = HeuristicWeightNormalization.Clamp(
                (handCardCount - DefaultStartCutoff) / (double)DefaultStartCutoff,
                0d,
                1d);
            var shorterSignal = 1d - HeuristicWeightNormalization.NormalizeRatio(trick.Cards.Count - 1, handCardCount - 1);

            var baseScore = HeuristicWeightNormalization.BaseScore(phaseSignal * shorterSignal, BaseWeight);
            return HeuristicWeightNormalization.ApplyWeight(baseScore, Weight);
        }
    }
}
