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
        private static readonly IReadOnlyList<Card> EmptyCards = Array.Empty<Card>();
        private static readonly IReadOnlyList<Card[]> EmptyCombinations = Array.Empty<Card[]>();

        private HandIndex(
            List<Card> cards,
            List<Card> wildCards,
            List<Card> nonWildCards,
            IReadOnlyList<Card>[] allCardsByRank,
            IReadOnlyList<Card>[] nonWildCardsByRank,
            IReadOnlyList<Card>[] nonWildCardsBySuit,
            Card[,] nonWildCardsBySuitRankLookup,
            int[,] nonWildPrefixCountsBySuitRank,
            IReadOnlyList<Rank>[] ranksWithAtLeastNNonWild,
            IReadOnlyList<Rank>[] ranksWithAtLeastNAll,
            IReadOnlyList<Card[]>[,] sameRankCombinationsByRankAndSize)
        {
            Cards = cards;
            WildCards = wildCards;
            NonWildCards = nonWildCards;
            AllCardsByRank = allCardsByRank;
            NonWildCardsByRank = nonWildCardsByRank;
            NonWildCardsBySuit = nonWildCardsBySuit;
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
        private IReadOnlyList<Card>[] AllCardsByRank { get; }
        private IReadOnlyList<Card>[] NonWildCardsByRank { get; }
        private IReadOnlyList<Card>[] NonWildCardsBySuit { get; }
        public IReadOnlyList<Rank>[] RanksWithAtLeastNNonWild { get; }
        public IReadOnlyList<Rank>[] RanksWithAtLeastNAll { get; }
        private IReadOnlyList<Card[]>[,] SameRankCombinationsByRankAndSize { get; }
        private Card[,] NonWildCardsBySuitRankLookup { get; }
        private int[,] NonWildPrefixCountsBySuitRank { get; }

        public static HandIndex Build(IEnumerable<Card> cards)
        {
            var cardList = cards as List<Card> ?? (cards ?? Array.Empty<Card>()).ToList();
            var wildCards = new List<Card>();
            var nonWildCards = new List<Card>();
            var allCardsByRank = new List<Card>[14];
            var nonWildCardsByRank = new List<Card>[14];
            var nonWildCardsBySuit = new List<Card>[6];
            var nonWildCardsBySuitRankLookup = new Card[6, 14];
            var nonWildPrefixCountsBySuitRank = new int[6, 14];
            var nonWildCountsByRank = new int[14];
            var allCountsByRank = new int[14];

            foreach (var suit in AllSuits)
            {
                nonWildCardsBySuit[(int)suit] = new List<Card>();
            }

            foreach (var card in cardList)
            {
                AddToRankIndex(allCardsByRank, allCountsByRank, card);

                if (card.IsWild)
                {
                    wildCards.Add(card);
                }
                else
                {
                    nonWildCards.Add(card);
                    nonWildCardsBySuit[(int)card.Suit].Add(card);
                    nonWildCardsBySuitRankLookup[(int)card.Suit, (int)card.Rank] = card;
                    AddToRankIndex(nonWildCardsByRank, nonWildCountsByRank, card);
                }
            }

            foreach (var suit in AllSuits)
            {
                nonWildCardsBySuit[(int)suit].Sort();
            }

            wildCards.Sort((left, right) => left.BaseRank.CompareTo(right.BaseRank));
            nonWildCards.Sort((left, right) => left.CompareBySuitAndRank(right));
            SortRankBuckets(allCardsByRank);
            SortRankBuckets(nonWildCardsByRank);

            BuildSuitRankPrefixCounts(nonWildCardsBySuitRankLookup, nonWildPrefixCountsBySuitRank);
            var ranksWithAtLeastNNonWild = BuildRanksWithAtLeastN(nonWildCountsByRank);
            var ranksWithAtLeastNAll = BuildRanksWithAtLeastN(allCountsByRank);
            var sameRankCombinationsByRankAndSize = BuildSameRankCombinations(nonWildCardsByRank);

            return new HandIndex(
                cardList,
                wildCards,
                nonWildCards,
                allCardsByRank,
                nonWildCardsByRank,
                nonWildCardsBySuit,
                nonWildCardsBySuitRankLookup,
                nonWildPrefixCountsBySuitRank,
                ranksWithAtLeastNNonWild,
                ranksWithAtLeastNAll,
                sameRankCombinationsByRankAndSize);
        }

        public bool ContainsNonWildCards(Rank rank, int minimumCount)
        {
            var cards = NonWildCardsByRank[(int)rank];
            return cards != null && cards.Count >= minimumCount;
        }

        public IReadOnlyList<Card> GetAllCardsByRank(Rank rank)
        {
            return AllCardsByRank[(int)rank] ?? EmptyCards;
        }

        public IReadOnlyList<Card> GetNonWildCardsByRank(Rank rank)
        {
            return NonWildCardsByRank[(int)rank] ?? EmptyCards;
        }

        public IReadOnlyList<Card> GetNonWildCardsBySuit(Suit suit)
        {
            return NonWildCardsBySuit[(int)suit] ?? EmptyCards;
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
            if (size < 1 || size >= SameRankCombinationsByRankAndSize.GetLength(1))
            {
                return EmptyCombinations;
            }

            return SameRankCombinationsByRankAndSize[(int)rank, size] ?? EmptyCombinations;
        }

        private static void AddToRankIndex(List<Card>[] index, int[] countsByRank, Card card)
        {
            var rankIndex = (int)card.Rank;
            var cards = index[rankIndex];
            if (cards == null)
            {
                cards = new List<Card>();
                index[rankIndex] = cards;
            }

            cards.Add(card);
            countsByRank[rankIndex]++;
        }

        private static void SortRankBuckets(List<Card>[] cardsByRank)
        {
            for (var rankValue = (int)Rank.TWO; rankValue <= (int)Rank.KING; rankValue++)
            {
                cardsByRank[rankValue]?.Sort((left, right) => left.CompareBySuitAndRank(right));
            }
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

        private static IReadOnlyList<Rank>[] BuildRanksWithAtLeastN(int[] countsByRank)
        {
            var ranksWithAtLeastN = new IReadOnlyList<Rank>[7];
            var buckets = new List<Rank>[7];

            for (var size = 0; size < buckets.Length; size++)
            {
                buckets[size] = new List<Rank>();
            }

            for (var rankValue = (int)Rank.TWO; rankValue <= (int)Rank.KING; rankValue++)
            {
                var upperBound = Math.Min(countsByRank[rankValue], buckets.Length - 1);
                for (var size = 1; size <= upperBound; size++)
                {
                    buckets[size].Add((Rank)rankValue);
                }
            }

            for (var size = 0; size < ranksWithAtLeastN.Length; size++)
            {
                ranksWithAtLeastN[size] = buckets[size].ToArray();
            }

            return ranksWithAtLeastN;
        }

        private static IReadOnlyList<Card[]>[,] BuildSameRankCombinations(List<Card>[] cardsByRank)
        {
            var result = new IReadOnlyList<Card[]>[14, 7];

            for (var rankValue = (int)Rank.TWO; rankValue <= (int)Rank.KING; rankValue++)
            {
                var cards = cardsByRank[rankValue];
                if (cards == null)
                {
                    continue;
                }

                var maxCombinationSize = Math.Min(6, cards.Count);

                for (var size = 1; size <= maxCombinationSize; size++)
                {
                    result[rankValue, size] = BuildCardCombinations(cards, size);
                }
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
            BuildCardCombinations(cards, length, 0, 0, buffer, combinations);
            return combinations;
        }

        private static void BuildCardCombinations(
            List<Card> cards,
            int length,
            int startIndex,
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

            for (var index = startIndex; index <= cards.Count - (length - depth); index++)
            {
                buffer[depth] = cards[index];
                BuildCardCombinations(cards, length, index + 1, depth + 1, buffer, combinations);
            }
        }
    }
}
