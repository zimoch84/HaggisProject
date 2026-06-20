using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PreferUsingWildAsHigherCardInContinuationWeightStrategy : IContinuationTrickWeightStrategy
    {
        private int PreferUsingWildAsHigherCardInContinuationWeight { get; }

        public PreferUsingWildAsHigherCardInContinuationWeightStrategy(int preferUsingWildAsHigherCardInContinuationWeight)
        {
            PreferUsingWildAsHigherCardInContinuationWeight = preferUsingWildAsHigherCardInContinuationWeight;
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
            if (trick == null || PreferUsingWildAsHigherCardInContinuationWeight <= 0)
            {
                return 0;
            }

            var currentWildRanks = GetEffectiveWildRanks(trick);
            if (currentWildRanks.Count == 0)
            {
                return 0;
            }

            var bestAlternativeWildRanks = (allSuggestedTricks ?? new List<Trick>())
                .Where(candidate => !ReferenceEquals(candidate, trick))
                .Where(candidate => candidate != null)
                .Where(candidate => candidate.Type == trick.Type)
                .Where(candidate => candidate.Cards.Count == trick.Cards.Count)
                .Where(candidate => candidate.Cards.Count(card => card.IsWild) == currentWildRanks.Count)
                .Where(candidate => candidate.CompareTo(trick) > 0)
                .Select(GetEffectiveWildRanks)
                .Where(candidateWildRanks => CompareLexicographically(candidateWildRanks, currentWildRanks) > 0)
                .OrderByDescending(candidateWildRanks => candidateWildRanks, LexicographicRanksComparer.Instance)
                .FirstOrDefault();

            if (bestAlternativeWildRanks == null)
            {
                return 0;
            }

            var totalRankImprovement = CalculateTotalRankImprovement(currentWildRanks, bestAlternativeWildRanks);
            return totalRankImprovement <= 0
                ? 0
                : -(totalRankImprovement * PreferUsingWildAsHigherCardInContinuationWeight);
        }

        private static List<int> GetEffectiveWildRanks(Trick trick)
        {
            return trick.Cards
                .Where(card => card.IsWild)
                .Select(card => (int)card.Rank)
                .OrderBy(rank => rank)
                .ToList();
        }

        private static int CompareLexicographically(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            var length = left.Count < right.Count ? left.Count : right.Count;
            for (var index = 0; index < length; index++)
            {
                if (left[index] != right[index])
                {
                    return left[index].CompareTo(right[index]);
                }
            }

            return left.Count.CompareTo(right.Count);
        }

        private static int CalculateTotalRankImprovement(IReadOnlyList<int> currentWildRanks, IReadOnlyList<int> betterWildRanks)
        {
            var length = currentWildRanks.Count < betterWildRanks.Count ? currentWildRanks.Count : betterWildRanks.Count;
            var total = 0;
            for (var index = 0; index < length; index++)
            {
                var delta = betterWildRanks[index] - currentWildRanks[index];
                if (delta > 0)
                {
                    total += delta;
                }
            }

            return total;
        }

        private sealed class LexicographicRanksComparer : IComparer<IReadOnlyList<int>>
        {
            public static LexicographicRanksComparer Instance { get; } = new LexicographicRanksComparer();

            public int Compare(IReadOnlyList<int> x, IReadOnlyList<int> y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return CompareLexicographically(x, y);
            }
        }
    }
}
