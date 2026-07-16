using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Enums;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PreferTricksThatAreMostLikelyNonBreakableWeightStrategy : IStartingTrickWeightStrategy
    {
        private const int MinNaturalRank = (int)Rank.TWO;
        private const int MaxNaturalRank = (int)Rank.TEN;
        private const int BaseWeight = 25;

        private float Weight { get; }

        public PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(float weight)
        {
            Weight = weight;
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
                Weight <= 0 ||
                trick.Cards.Count == 0)
            {
                return 0;
            }

            if (trick.Type != TrickType.SINGLE &&
                trick.Type != TrickType.PAIR &&
                trick.Type != TrickType.TRIPLE &&
                trick.Type != TrickType.QUAD &&
                trick.Type != TrickType.FIVED &&
                trick.Type != TrickType.SIXED
                )
            {
                return 0;
            }

            var probability = CalculateProbabilityThatTrickIsNonBreakable(trick, gameState);
            if (probability < 1d)
            {
                return 0;
            }

            return HeuristicWeightNormalization.ApplyWeight(BaseWeight, Weight);
        }

        private static double CalculateProbabilityThatTrickIsNonBreakable(Trick trick, RoundState gameState)
        {
            var requiredCards = trick.Cards.Count;
            var baseRank = (int)trick.Cards[0].Rank;
            var higherRanks = Enumerable.Range(baseRank + 1, (int)Rank.KING - baseRank).ToList();
            if (higherRanks.Count == 0)
            {
                return 1d;
            }

            var unavailableByRank = BuildUnavailableCardCountsByRank(gameState);
            var remainingWilds = CountRemainingWilds(unavailableByRank);
            var exhaustedHigherRanks = higherRanks.Count(rank =>
                GetRemainingNaturalCardsForRank(rank, unavailableByRank) + remainingWilds < requiredCards);

            return exhaustedHigherRanks / (double)higherRanks.Count;
        }

        private static Dictionary<int, int> BuildUnavailableCardCountsByRank(RoundState gameState)
        {
            var unavailableCards = new List<Card>();

            foreach (var player in gameState.Players ?? new List<IHaggisPlayer>())
            {
                unavailableCards.AddRange(player.Discard ?? new List<Card>());
            }

            unavailableCards.AddRange(
                gameState.CurrentTrickPlay?.Actions?
                    .Where(action => !action.IsPass && action.Trick != null)
                    .SelectMany(action => action.Trick.Cards)
                    .ToList()
                ?? new List<Card>());

            return unavailableCards
                .GroupBy(card => (int)card.BaseRank)
                .ToDictionary(group => group.Key, group => group.Count());
        }

        private static int CountRemainingWilds(Dictionary<int, int> unavailableByRank)
        {
            var unavailableWilds = GetUnavailableCount((int)Rank.JACK, unavailableByRank)
                + GetUnavailableCount((int)Rank.QUEEN, unavailableByRank)
                + GetUnavailableCount((int)Rank.KING, unavailableByRank);

            return Math.Max(0, 3 - unavailableWilds);
        }

        private static int GetRemainingNaturalCardsForRank(int rank, Dictionary<int, int> unavailableByRank)
        {
            var totalCardsForRank = rank <= MaxNaturalRank ? 5 : 1;
            return Math.Max(0, totalCardsForRank - GetUnavailableCount(rank, unavailableByRank));
        }

        private static int GetUnavailableCount(int rank, Dictionary<int, int> unavailableByRank)
        {
            return unavailableByRank.TryGetValue(rank, out var count) ? count : 0;
        }
    }
}
