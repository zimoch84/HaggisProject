using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PenalizeContinuationWhenHigherRelatedCombinationExistsWeightStrategy : IContinuationTrickWeightStrategy
    {
        private int HigherRelatedCombinationContinuationPenaltyFactor { get; }

        public PenalizeContinuationWhenHigherRelatedCombinationExistsWeightStrategy(int higherRelatedCombinationContinuationPenaltyFactor)
        {
            HigherRelatedCombinationContinuationPenaltyFactor = higherRelatedCombinationContinuationPenaltyFactor;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            var tricks = allSuggestedTricks ?? new List<Trick>();
            return tricks
                .Select(trick => (GetWeightForTrick(trick, tricks, gameState), trick))
                .ToList();
        }

        private int GetWeightForTrick(Trick trick, List<Trick> allSuggestedTricks, RoundState gameState)
        {
            if (trick == null ||
                gameState?.CurrentPlayer?.Hand == null ||
                HigherRelatedCombinationContinuationPenaltyFactor <= 0)
            {
                return 0;
            }

            var trickClass = trick.Type.Class();
            if (trickClass == TrickClass.ELSE)
            {
                return 0;
            }

            var candidateNaturalCards = trick.Cards.Where(card => !card.IsWild).ToList();
            if (candidateNaturalCards.Count == 0)
            {
                return 0;
            }

            var hasHigherRelatedCombination = allSuggestedTricks
                .Where(candidate => !ReferenceEquals(candidate, trick))
                .Where(candidate => candidate.Type.Class() == trickClass)
                .Where(candidate => candidate.Cards.Count > trick.Cards.Count)
                .Any(candidate => candidateNaturalCards.All(card => candidate.Cards.Contains(card)));

            return hasHigherRelatedCombination
                ? -(gameState.CurrentPlayer.Hand.Count * HigherRelatedCombinationContinuationPenaltyFactor)
                : 0;
        }
    }
}
