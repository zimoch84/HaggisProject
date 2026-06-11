using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PenalizeWildCardsInOpeningWeightStrategy : IStartingTrickWeightStrategy
    {
        private int WildCardOpeningPenaltyFactor { get; }

        public PenalizeWildCardsInOpeningWeightStrategy(int wildCardOpeningPenaltyFactor)
        {
            WildCardOpeningPenaltyFactor = wildCardOpeningPenaltyFactor;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (GetWeightForTrick(trick, allSuggestedTricks, gameState), trick))
                .ToList();
        }

        private int GetWeightForTrick(Trick trick, List<Trick> allSuggestedTricks, RoundState gameState)
        {
            if (trick == null || gameState?.CurrentPlayer?.Hand == null || WildCardOpeningPenaltyFactor <= 0)
            {
                return 0;
            }

            var wildCardsUsed = trick.Cards.Count(card => card.IsWild);
            if (wildCardsUsed == 0)
            {
                return 0;
            }

            var penalty = wildCardsUsed * gameState.CurrentPlayer.Hand.Count * WildCardOpeningPenaltyFactor;
            if (HasEquivalentTrickWithLowerWilds(trick, allSuggestedTricks))
            {
                penalty *= 2;
            }

            return -penalty;
        }

        private static bool HasEquivalentTrickWithLowerWilds(Trick trick, List<Trick> allSuggestedTricks)
        {
            var currentWildRanks = trick.Cards
                .Where(card => card.IsWild)
                .Select(card => (int)card.BaseRank)
                .OrderBy(rank => rank)
                .ToArray();
            if (currentWildRanks.Length == 0)
            {
                return false;
            }

            var effectiveRanks = trick.Cards
                .Select(card => (int)card.Rank)
                .OrderBy(rank => rank)
                .ToArray();
            var nonWildCards = trick.Cards
                .Where(card => !card.IsWild)
                .Select(card => card.ToString())
                .OrderBy(card => card)
                .ToArray();

            return (allSuggestedTricks ?? new List<Trick>())
                .Where(candidate => !ReferenceEquals(candidate, trick))
                .Where(candidate => candidate.Type == trick.Type)
                .Where(candidate => candidate.Cards.Count == trick.Cards.Count)
                .Where(candidate => candidate.Cards.Count(card => card.IsWild) == currentWildRanks.Length)
                .Where(candidate => candidate.Cards
                    .Select(card => (int)card.Rank)
                    .OrderBy(rank => rank)
                    .SequenceEqual(effectiveRanks))
                .Where(candidate => candidate.Cards
                    .Where(card => !card.IsWild)
                    .Select(card => card.ToString())
                    .OrderBy(card => card)
                    .SequenceEqual(nonWildCards))
                .Select(candidate => candidate.Cards
                    .Where(card => card.IsWild)
                    .Select(card => (int)card.BaseRank)
                    .OrderBy(rank => rank)
                    .ToArray())
                .Any(candidateWildRanks => IsLexicographicallyLower(candidateWildRanks, currentWildRanks));
        }

        private static bool IsLexicographicallyLower(int[] left, int[] right)
        {
            for (var index = 0; index < left.Length && index < right.Length; index++)
            {
                if (left[index] < right[index])
                {
                    return true;
                }

                if (left[index] > right[index])
                {
                    return false;
                }
            }

            return false;
        }
    }
}
