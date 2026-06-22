using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PreferLowerValueContinuationWeightStrategy : IContinuationTrickWeightStrategy
    {
        private const int BaseWeight = 20;

        private float Weight { get; }

        public PreferLowerValueContinuationWeightStrategy(float weight)
        {
            Weight = weight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (GetWeightForTrick(trick, allSuggestedTricks, gameState), trick))
                .ToList();
        }

        private int GetWeightForTrick(Trick trick, List<Trick> allSuggestedTricks, RoundState gameState)
        {
            var lastTrick = gameState?.CurrentTrickPlay?.LastNotPassTrick;
            if (trick == null || lastTrick == null || Weight <= 0)
            {
                return 0;
            }

            if (trick.Type == TrickType.BOMB)
            {
                return 0;
            }

            var comparableTricks = (allSuggestedTricks ?? new List<Trick>())
                .Where(candidate => candidate != null)
                .Where(candidate => candidate.Type == trick.Type)
                .ToList();
            if (comparableTricks.Count <= 1)
            {
                return 0;
            }

            var orderedRanks = comparableTricks
                .Select(GetComparableRank)
                .Distinct()
                .OrderBy(rank => rank)
                .ToList();
            if (orderedRanks.Count <= 1)
            {
                return 0;
            }

            var trickRank = GetComparableRank(trick);
            var trickIndex = orderedRanks.FindIndex(rank => rank == trickRank);
            if (trickIndex < 0)
            {
                return 0;
            }

            var lowerValueOpportunity = (orderedRanks.Count - 1) - trickIndex;
            if (lowerValueOpportunity <= 0)
            {
                return 0;
            }

            var maxComparableOpportunity = orderedRanks.Count - 1;
            var baseScore = HeuristicWeightNormalization.BaseScore(
                HeuristicWeightNormalization.NormalizeRatio(lowerValueOpportunity, maxComparableOpportunity),
                BaseWeight);
            return HeuristicWeightNormalization.ApplyWeight(baseScore, Weight);
        }

        private static int GetComparableRank(Trick trick)
        {
            return (int)trick.Cards.First().Rank;
        }
    }
}
