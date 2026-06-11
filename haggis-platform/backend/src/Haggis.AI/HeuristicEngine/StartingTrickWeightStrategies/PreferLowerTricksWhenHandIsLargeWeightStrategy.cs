using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PreferLowerTricksWhenHandIsLargeWeightStrategy : IStartingTrickWeightStrategy
    {
        private const int MinRankValue = (int)Rank.TWO;
        private const int MaxRankValue = (int)Rank.KING;

        private int PreferLowerStartCutoff { get; }
        private int PreferLowerStartMaxWeight { get; }

        public PreferLowerTricksWhenHandIsLargeWeightStrategy(
            int preferLowerStartCutoff,
            int preferLowerStartMaxWeight)
        {
            PreferLowerStartCutoff = preferLowerStartCutoff;
            PreferLowerStartMaxWeight = preferLowerStartMaxWeight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (GetWeightForTrick(trick, gameState), trick))
                .ToList();
        }

        private int GetWeightForTrick(Trick trick, RoundState gameState)
        {
            if (trick == null ||
                gameState?.CurrentPlayer?.Hand == null ||
                PreferLowerStartMaxWeight <= 0 ||
                PreferLowerStartCutoff <= 0 ||
                trick.Cards.Count == 0)
            {
                return 0;
            }

            var handCardCount = gameState.CurrentPlayer.Hand.Count;
            var phaseBias = Clamp(
                (handCardCount - PreferLowerStartCutoff) / (double)PreferLowerStartCutoff,
                -1d,
                1d);
            var averageRank = trick.Cards.Average(card => (int)card.Rank);
            var rankBias = 1d - (2d * (averageRank - MinRankValue) / (MaxRankValue - MinRankValue));

            return (int)Math.Round(PreferLowerStartMaxWeight * phaseBias * rankBias, MidpointRounding.AwayFromZero);
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
