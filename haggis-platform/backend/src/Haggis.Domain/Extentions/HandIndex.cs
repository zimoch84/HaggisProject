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
        private const int MinRankValue = (int)Rank.TWO;
        private const int MaxRankValue = (int)Rank.KING;

        private HandIndex(
            List<Card> cards,
            List<Card> wildCards,
            List<Card> nonWildCards,
            IReadOnlyList<Card>[] allCardsByRank,
            IReadOnlyList<Card>[] nonWildCardsByRank,
            IReadOnlyList<Card>[] nonWildCardsBySuit,
            Card[,] nonWildCardsBySuitRankLookup,
            ushort[] nonWildSuitMasks,
            ushort nonWildRankMask,
            byte[] nonWildCountsByRank,
            byte[] allCountsByRank)
        {
            Cards = cards;
            WildCards = wildCards;
            NonWildCards = nonWildCards;
            AllCardsByRank = allCardsByRank;
            NonWildCardsByRank = nonWildCardsByRank;
            NonWildCardsBySuit = nonWildCardsBySuit;
            NonWildCardsBySuitRankLookup = nonWildCardsBySuitRankLookup;
            NonWildSuitMasks = nonWildSuitMasks;
            NonWildRankMask = nonWildRankMask;
            NonWildCountsByRank = nonWildCountsByRank;
            AllCountsByRank = allCountsByRank;
        }

        public List<Card> Cards { get; }
        public List<Card> WildCards { get; }
        public int WildCardCount => WildCards.Count;
        public List<Card> NonWildCards { get; }
        private IReadOnlyList<Card>[] AllCardsByRank { get; }
        private IReadOnlyList<Card>[] NonWildCardsByRank { get; }
        private IReadOnlyList<Card>[] NonWildCardsBySuit { get; }
        private Card[,] NonWildCardsBySuitRankLookup { get; }
        private ushort[] NonWildSuitMasks { get; }
        private ushort NonWildRankMask { get; }
        private byte[] NonWildCountsByRank { get; }
        private byte[] AllCountsByRank { get; }

        public static HandIndex Build(IEnumerable<Card> cards)
        {
            var cardList = cards as List<Card> ?? (cards ?? Array.Empty<Card>()).ToList();
            var wildCards = new List<Card>();
            var nonWildCards = new List<Card>();
            var allCardsByRank = new List<Card>[14];
            var nonWildCardsByRank = new List<Card>[14];
            var nonWildCardsBySuit = new List<Card>[6];
            var nonWildCardsBySuitRankLookup = new Card[6, 14];
            var nonWildSuitMasks = new ushort[6];
            var nonWildCountsByRank = new byte[14];
            var allCountsByRank = new byte[14];
            ushort nonWildRankMask = 0;

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
                    nonWildSuitMasks[(int)card.Suit] |= GetRankBit(card.Rank);
                    nonWildRankMask |= GetRankBit(card.Rank);
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

            return new HandIndex(
                cardList,
                wildCards,
                nonWildCards,
                allCardsByRank,
                nonWildCardsByRank,
                nonWildCardsBySuit,
                nonWildCardsBySuitRankLookup,
                nonWildSuitMasks,
                nonWildRankMask,
                nonWildCountsByRank,
                allCountsByRank);
        }

        public bool ContainsNonWildCards(Rank rank, int minimumCount)
        {
            return NonWildCountsByRank[(int)rank] >= minimumCount;
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

        public ushort GetNonWildSuitMask(Suit suit)
        {
            return NonWildSuitMasks[(int)suit];
        }

        public ushort GetNonWildRankMask()
        {
            return NonWildRankMask;
        }

        public int GetNonWildCount(Rank rank)
        {
            return NonWildCountsByRank[(int)rank];
        }

        public int GetAllCount(Rank rank)
        {
            return AllCountsByRank[(int)rank];
        }

        public bool HasNonWildCard(Suit suit, Rank rank)
        {
            return (NonWildSuitMasks[(int)suit] & GetRankBit(rank)) != 0;
        }

        public bool TryGetNonWildCard(Suit suit, Rank rank, out Card card)
        {
            if (!HasNonWildCard(suit, rank))
            {
                card = null;
                return false;
            }

            card = NonWildCardsBySuitRankLookup[(int)suit, (int)rank];
            return card != null;
        }

        public int GetNonWildCountInRange(Suit suit, Rank firstRank, int length)
        {
            if (length <= 0)
            {
                return 0;
            }

            var windowMask = CreateRankWindow(firstRank, length);
            return CountBits((ushort)(NonWildSuitMasks[(int)suit] & windowMask));
        }

        public IReadOnlyList<Rank> GetRanksWithAtLeastNNonWild(int minimumCount)
        {
            return BuildRanksWithAtLeastN(NonWildCountsByRank, minimumCount);
        }

        public IReadOnlyList<Rank> GetRanksWithAtLeastNAll(int minimumCount)
        {
            return BuildRanksWithAtLeastN(AllCountsByRank, minimumCount);
        }

        public IReadOnlyList<Card[]> GetSameRankCombinations(Rank rank, int size)
        {
            if (size < 1)
            {
                return EmptyCombinations;
            }

            return BuildCardCombinations(GetNonWildCardsByRank(rank), size);
        }

        private static void AddToRankIndex(List<Card>[] index, byte[] countsByRank, Card card)
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

        private static IReadOnlyList<Rank> BuildRanksWithAtLeastN(byte[] countsByRank, int minimumCount)
        {
            if (minimumCount < 1)
            {
                return EmptyRanks;
            }

            var ranks = new List<Rank>();
            for (var rankValue = (int)Rank.TWO; rankValue <= (int)Rank.KING; rankValue++)
            {
                if (countsByRank[rankValue] >= minimumCount)
                {
                    ranks.Add((Rank)rankValue);
                }
            }

            return ranks.Count == 0 ? EmptyRanks : ranks.ToArray();
        }

        private static IReadOnlyList<Card[]> BuildCardCombinations(IReadOnlyList<Card> cards, int length)
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
            IReadOnlyList<Card> cards,
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

        private static ushort GetRankBit(Rank rank)
        {
            return (ushort)(1 << ((int)rank - MinRankValue));
        }

        private static ushort CreateRankWindow(Rank firstRank, int length)
        {
            var startBit = (int)firstRank - MinRankValue;
            var widthMask = (1 << length) - 1;
            return (ushort)(widthMask << startBit);
        }

        private static int CountBits(ushort value)
        {
            var count = 0;
            while (value != 0)
            {
                value = (ushort)(value & (value - 1));
                count++;
            }

            return count;
        }
    }
}
