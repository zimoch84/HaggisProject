using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PenalizeWildCardsInContinuationWeightStrategy : IContinuationTrickWeightStrategy
    {
        private const int MaxPenaltyCap = 50;
        private int WildCardContinuationPenaltyFactor { get; }

        public PenalizeWildCardsInContinuationWeightStrategy(int wildCardContinuationPenaltyFactor)
        {
            WildCardContinuationPenaltyFactor = wildCardContinuationPenaltyFactor;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            var handCount = gameState?.CurrentPlayer?.Hand?.Count ?? 0;
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick =>
                {
                    var wildCount = trick?.Cards.Count(card => card.IsWild) ?? 0;
                    var weight = wildCount == 0
                        ? 0
                        : -(handCount * wildCount * WildCardContinuationPenaltyFactor);
                    if (weight < -MaxPenaltyCap)
                    {
                        weight = -MaxPenaltyCap;
                    }

                    return (weight, trick);
                })
                .ToList();
        }
    }
}
