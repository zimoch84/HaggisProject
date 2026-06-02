using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Domain.Extentions
{
    public sealed class HandIndex
    {
        private static readonly Suit[] AllSuits = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToArray();
        private static readonly IReadOnlyList<Rank> EmptyRanks = Array.Empty<Rank>();
        private static readonly IReadOnlyList<Card[]> EmptyCombinations = Array.Empty<Card[]>();

        private HandIndex(
            List<Card> cards,
            List<Card> wildCards,
            List<Card> nonWildCards,
            Dictionary<Rank, List<Card>> allCardsByRank,
            Dictionary<Rank, List<Card>> nonWildCardsByRank,
            Dictionary<Suit, List<Card>> nonWildCardsBySuit,
            Dictionary<Suit, Dictionary<Rank, Card>> nonWildCardsBySuitAndRank,
            Card[,] nonWildCardsBySuitRankLookup,
            int[,] nonWildPrefixCountsBySuitRank,
            IReadOnlyList<Rank>[] ranksWithAtLeastNNonWild,
            IReadOnlyList<Rank>[] ranksWithAtLeastNAll,
            Dictionary<Rank, IReadOnlyDictionary<int, IReadOnlyList<Card[]>>> sameRankCombinationsByRankAndSize)
        {
            Cards = cards;
            WildCards = wildCards;
            NonWildCards = nonWildCards;
            AllCardsByRank = allCardsByRank;
            NonWildCardsByRank = nonWildCardsByRank;
            NonWildCardsBySuit = nonWildCardsBySuit;
            NonWildCardsBySuitAndRank = nonWildCardsBySuitAndRank;
            NonWildCardsBySuitRankLookup = nonWildCardsBySuitRankLookup;
            NonWildPrefixCountsBySuitRank = nonWildPrefixCountsBySuitRank;
            RanksWithAtLeastNNonWild = ranksWithAtLeastNNonWild;
            RanksWithAtLeastNAll = ranksWithAtLeastNAll;
            SameRankCombinationsByRankAndSize = sameRankCombinationsByRankAndSize;
        }

        public List<Card> Cards { get; }
        public List<Card> WildCards { get; }
        public int WildCardCount => WildCards.Count;
        public List<Card> NonWildCards { get; }
        public Dictionary<Rank, List<Card>> AllCardsByRank { get; }
        public Dictionary<Rank, List<Card>> NonWildCardsByRank { get; }
        public Dictionary<Suit, List<Card>> NonWildCardsBySuit { get; }
        public Dictionary<Suit, Dictionary<Rank, Card>> NonWildCardsBySuitAndRank { get; }
        public IReadOnlyList<Rank>[] RanksWithAtLeastNNonWild { get; }
        public IReadOnlyList<Rank>[] RanksWithAtLeastNAll { get; }
        public IReadOnlyDictionary<Rank, IReadOnlyDictionary<int, IReadOnlyList<Card[]>>> SameRankCombinationsByRankAndSize { get; }
        private Card[,] NonWildCardsBySuitRankLookup { get; }
        private int[,] NonWildPrefixCountsBySuitRank { get; }

        public static HandIndex Build(IEnumerable<Card> cards)
        {
            var cardList = (cards ?? Array.Empty<Card>()).ToList();
            var wildCards = new List<Card>();
            var nonWildCards = new List<Card>();
            var allCardsByRank = new Dictionary<Rank, List<Card>>();
            var nonWildCardsByRank = new Dictionary<Rank, List<Card>>();
            var nonWildCardsBySuit = new Dictionary<Suit, List<Card>>();
            var nonWildCardsBySuitAndRank = new Dictionary<Suit, Dictionary<Rank, Card>>();
            var nonWildCardsBySuitRankLookup = new Card[6, 14];
            var nonWildPrefixCountsBySuitRank = new int[6, 14];

            foreach (var suit in AllSuits)
            {
                nonWildCardsBySuit[suit] = new List<Card>();
                nonWildCardsBySuitAndRank[suit] = new Dictionary<Rank, Card>();
            }

            foreach (var card in cardList)
            {
                if (card.IsWild)
                {
                    wildCards.Add(card);
                }
                else
                {
                    nonWildCards.Add(card);
                    nonWildCardsBySuit[card.Suit].Add(card);
                    nonWildCardsBySuitAndRank[card.Suit][card.Rank] = card;
                    nonWildCardsBySuitRankLookup[(int)card.Suit, (int)card.Rank] = card;
                    AddToRankIndex(nonWildCardsByRank, card);
                }

                AddToRankIndex(allCardsByRank, card);
            }

            foreach (var suit in AllSuits)
            {
                nonWildCardsBySuit[suit].Sort();
            }

            wildCards.Sort((left, right) => left.BaseRank.CompareTo(right.BaseRank));
            nonWildCards.Sort((left, right) => left.CompareBySuitAndRank(right));

            BuildSuitRankPrefixCounts(nonWildCardsBySuitRankLookup, nonWildPrefixCountsBySuitRank);
            var ranksWithAtLeastNNonWild = BuildRanksWithAtLeastN(nonWildCardsByRank);
            var ranksWithAtLeastNAll = BuildRanksWithAtLeastN(allCardsByRank);
            var sameRankCombinationsByRankAndSize = BuildSameRankCombinations(nonWildCardsByRank);

            return new HandIndex(
                cardList,
                wildCards,
                nonWildCards,
                allCardsByRank,
                nonWildCardsByRank,
                nonWildCardsBySuit,
                nonWildCardsBySuitAndRank,
                nonWildCardsBySuitRankLookup,
                nonWildPrefixCountsBySuitRank,
                ranksWithAtLeastNNonWild,
                ranksWithAtLeastNAll,
                sameRankCombinationsByRankAndSize);
        }

        public bool ContainsNonWildCards(Rank rank, int minimumCount)
        {
            return NonWildCardsByRank.TryGetValue(rank, out var cards) && cards.Count >= minimumCount;
        }

        public List<Card> GetAllCardsByRank(Rank rank)
        {
            return AllCardsByRank.TryGetValue(rank, out var cards) ? cards : new List<Card>();
        }

        public List<Card> GetNonWildCardsByRank(Rank rank)
        {
            return NonWildCardsByRank.TryGetValue(rank, out var cards) ? cards : new List<Card>();
        }

        public List<Card> GetNonWildCardsBySuit(Suit suit)
        {
            return NonWildCardsBySuit.TryGetValue(suit, out var cards) ? cards : new List<Card>();
        }

        public bool TryGetNonWildCard(Suit suit, Rank rank, out Card card)
        {
            card = NonWildCardsBySuitRankLookup[(int)suit, (int)rank];
            return card != null;
        }

        public int GetNonWildCountInRange(Suit suit, Rank firstRank, int length)
        {
            if (length <= 0)
            {
                return 0;
            }

            var suitIndex = (int)suit;
            var startRankValue = (int)firstRank;
            var endRankValue = startRankValue + length - 1;
            var beforeStart = startRankValue > (int)Rank.TWO
                ? NonWildPrefixCountsBySuitRank[suitIndex, startRankValue - 1]
                : 0;

            return NonWildPrefixCountsBySuitRank[suitIndex, endRankValue] - beforeStart;
        }

        public IReadOnlyList<Rank> GetRanksWithAtLeastNNonWild(int minimumCount)
        {
            return minimumCount >= 0 && minimumCount < RanksWithAtLeastNNonWild.Length
                ? RanksWithAtLeastNNonWild[minimumCount]
                : EmptyRanks;
        }

        public IReadOnlyList<Rank> GetRanksWithAtLeastNAll(int minimumCount)
        {
            return minimumCount >= 0 && minimumCount < RanksWithAtLeastNAll.Length
                ? RanksWithAtLeastNAll[minimumCount]
                : EmptyRanks;
        }

        public IReadOnlyList<Card[]> GetSameRankCombinations(Rank rank, int size)
        {
            if (!SameRankCombinationsByRankAndSize.TryGetValue(rank, out var combinationsBySize))
            {
                return EmptyCombinations;
            }

            return combinationsBySize.TryGetValue(size, out var combinations)
                ? combinations
                : EmptyCombinations;
        }

        private static void AddToRankIndex(Dictionary<Rank, List<Card>> index, Card card)
        {
            if (!index.TryGetValue(card.Rank, out var cards))
            {
                cards = new List<Card>();
                index[card.Rank] = cards;
            }

            cards.Add(card);
        }

        private static void BuildSuitRankPrefixCounts(Card[,] nonWildCardsBySuitRankLookup, int[,] nonWildPrefixCountsBySuitRank)
        {
            foreach (var suit in AllSuits)
            {
                var suitIndex = (int)suit;
                var runningCount = 0;

                for (var rankValue = (int)Rank.TWO; rankValue <= (int)Rank.KING; rankValue++)
                {
                    if (nonWildCardsBySuitRankLookup[suitIndex, rankValue] != null)
                    {
                        runningCount++;
                    }

                    nonWildPrefixCountsBySuitRank[suitIndex, rankValue] = runningCount;
                }
            }
        }

        private static IReadOnlyList<Rank>[] BuildRanksWithAtLeastN(Dictionary<Rank, List<Card>> cardsByRank)
        {
            var ranksWithAtLeastN = new IReadOnlyList<Rank>[7];
            var buckets = new List<Rank>[7];

            for (var size = 0; size < buckets.Length; size++)
            {
                buckets[size] = new List<Rank>();
            }

            foreach (var item in cardsByRank)
            {
                var upperBound = Math.Min(item.Value.Count, buckets.Length - 1);
                for (var size = 1; size <= upperBound; size++)
                {
                    buckets[size].Add(item.Key);
                }
            }

            for (var size = 0; size < ranksWithAtLeastN.Length; size++)
            {
                ranksWithAtLeastN[size] = buckets[size].ToArray();
            }

            return ranksWithAtLeastN;
        }

        private static Dictionary<Rank, IReadOnlyDictionary<int, IReadOnlyList<Card[]>>> BuildSameRankCombinations(
            Dictionary<Rank, List<Card>> cardsByRank)
        {
            var result = new Dictionary<Rank, IReadOnlyDictionary<int, IReadOnlyList<Card[]>>>();

            foreach (var item in cardsByRank)
            {
                var combinationsBySize = new Dictionary<int, IReadOnlyList<Card[]>>();
                var maxCombinationSize = Math.Min(6, item.Value.Count);

                for (var size = 1; size <= maxCombinationSize; size++)
                {
                    combinationsBySize[size] = BuildCardCombinations(item.Value, size);
                }

                result[item.Key] = combinationsBySize;
            }

            return result;
        }

        private static IReadOnlyList<Card[]> BuildCardCombinations(List<Card> cards, int length)
        {
            var combinations = new List<Card[]>();
            if (cards == null || length < 1 || cards.Count < length)
            {
                return combinations;
            }

            var buffer = new Card[length];
            BuildCardCombinations(cards, length, 0, buffer, combinations);
            return combinations;
        }

        private static void BuildCardCombinations(
            List<Card> cards,
            int length,
            int depth,
            Card[] buffer,
            List<Card[]> combinations)
        {
            if (depth == length)
            {
                var result = new Card[length];
                Array.Copy(buffer, result, length);
                combinations.Add(result);
                return;
            }

            for (var index = 0; index < cards.Count; index++)
            {
                var candidate = cards[index];
                if (depth > 0 && candidate.CompareBySuitAndRank(buffer[depth - 1]) <= 0)
                {
                    continue;
                }

                buffer[depth] = candidate;
                BuildCardCombinations(cards, length, depth + 1, buffer, combinations);
            }
        }
    }
}
