using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PreferLowerValueContinuationWeightStrategy : IContinuationTrickWeightStrategy
    {
        private int LowerValueContinuationWeight { get; }

        public PreferLowerValueContinuationWeightStrategy(int lowerValueContinuationWeight)
        {
            LowerValueContinuationWeight = lowerValueContinuationWeight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (GetWeightForTrick(trick, gameState), trick))
                .ToList();
        }

        private int GetWeightForTrick(Trick trick, RoundState gameState)
        {
            var lastTrick = gameState?.CurrentTrickPlay?.LastNotPassTrick;
            if (trick == null || lastTrick == null || LowerValueContinuationWeight <= 0)
            {
                return 0;
            }

            if (trick.Type == TrickType.BOMB && lastTrick.Type != TrickType.BOMB)
            {
                return -LowerValueContinuationWeight * 10;
            }

            var typeGap = trick.Type == lastTrick.Type
                ? 0
                : ((int)trick.Type - (int)lastTrick.Type) / 10;
            var rankGap = trick.FirstCard().RankDiff(lastTrick.FirstCard());

            return -(typeGap + rankGap) * LowerValueContinuationWeight;
        }
    }
}
