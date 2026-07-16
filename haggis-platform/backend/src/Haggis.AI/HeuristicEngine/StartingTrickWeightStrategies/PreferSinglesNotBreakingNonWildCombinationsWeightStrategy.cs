using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PreferSinglesNotBreakingNonWildCombinationsWeightStrategy : IStartingTrickWeightStrategy
    {
        private const int BaseWeight = 40;

        private float Weight { get; }

        public PreferSinglesNotBreakingNonWildCombinationsWeightStrategy(float weight)
        {
            Weight = weight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            var tricks = allSuggestedTricks ?? new List<Trick>();
            return tricks
                .Select(trick => (GetWeightForTrick(trick, tricks), trick))
                .ToList();
        }

        private int GetWeightForTrick(Trick trick, List<Trick> allSuggestedTricks)
        {
            if (trick == null || trick.Type != TrickType.SINGLE || trick.Cards.Count != 1)
            {
                return 0;
            }

            var singleCard = trick.Cards[0];
            if (singleCard.IsWild)
            {
                return 0;
            }

            var breaksNonWildCombination = (allSuggestedTricks ?? new List<Trick>())
                .Where(candidate => candidate.Type != TrickType.SINGLE)
                .Where(candidate => candidate.Cards.Count > 1)
                .Where(candidate => candidate.Cards.All(card => !card.IsWild))
                .Any(candidate => candidate.Cards.Contains(singleCard));

            return breaksNonWildCombination
                ? 0
                : HeuristicWeightNormalization.ApplyWeight(BaseWeight, Weight);
        }
    }
}
