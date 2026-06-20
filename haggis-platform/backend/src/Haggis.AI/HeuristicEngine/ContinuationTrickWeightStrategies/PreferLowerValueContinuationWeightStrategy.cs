using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PreferLowerValueContinuationWeightStrategy : IContinuationTrickWeightStrategy
    {
        private const int MaxWeightCap = 50;
        private int LowerValueContinuationWeight { get; }

        public PreferLowerValueContinuationWeightStrategy(int lowerValueContinuationWeight)
        {
            LowerValueContinuationWeight = lowerValueContinuationWeight;
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
            if (trick == null || lastTrick == null || LowerValueContinuationWeight <= 0)
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

            var orderedTricks = comparableTricks
                .OrderBy(candidate => candidate)
                .ToList();
            var trickIndex = orderedTricks.FindIndex(candidate => ReferenceEquals(candidate, trick));
            if (trickIndex < 0)
            {
                return 0;
            }

            var lowerValueOpportunity = (orderedTricks.Count - 1) - trickIndex;
            if (lowerValueOpportunity <= 0)
            {
                return 0;
            }

            var maxComparableOpportunity = orderedTricks.Count - 1;
            var normalizedWeight = (lowerValueOpportunity * MaxWeightCap * LowerValueContinuationWeight) /
                                   maxComparableOpportunity;

            return normalizedWeight > MaxWeightCap
                ? MaxWeightCap
                : normalizedWeight;
        }
    }
}
