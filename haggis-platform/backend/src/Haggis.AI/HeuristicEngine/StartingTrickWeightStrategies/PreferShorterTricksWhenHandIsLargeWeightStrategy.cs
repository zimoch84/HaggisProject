using System.Collections.Generic;
using System;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PreferShorterTricksWhenHandIsLargeWeightStrategy : IStartingTrickWeightStrategy
    {
        private int ShorterStartCutoff { get; }
        private int ShorterStartNormalization { get; }

        public PreferShorterTricksWhenHandIsLargeWeightStrategy(
            int shorterStartCutoff,
            int shorterStartNormalization)
        {
            ShorterStartCutoff = shorterStartCutoff;
            ShorterStartNormalization = shorterStartNormalization;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (GetWeightForTrick(trick, gameState), trick))
                .ToList();
        }

        private int GetWeightForTrick(Trick trick, RoundState gameState)
        {
            if (trick == null || gameState?.CurrentPlayer == null)
            {
                return 0;
            }

            var handCardCount = gameState.CurrentPlayer.Hand?.Count ?? 0;
            var trickLength = trick.Cards.Count;
            var raw = handCardCount - trickLength - ShorterStartCutoff;

            return handCardCount <= 0
                ? 0
                : ShorterStartNormalization * Math.Max(1, raw);
        }
    }
}
