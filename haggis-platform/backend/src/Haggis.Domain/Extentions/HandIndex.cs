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

        private HandIndex(
            List<Card> cards,
            List<Card> wildCards,
            List<Card> nonWildCards,
            Dictionary<Rank, List<Card>> allCardsByRank,
            Dictionary<Rank, List<Card>> nonWildCardsByRank,
            Dictionary<Suit, List<Card>> nonWildCardsBySuit,
            Dictionary<Suit, Dictionary<Rank, Card>> nonWildCardsBySuitAndRank,
            Card[,] nonWildCardsBySuitRankLookup)
        {
            Cards = cards;
            WildCards = wildCards;
            NonWildCards = nonWildCards;
            AllCardsByRank = allCardsByRank;
            NonWildCardsByRank = nonWildCardsByRank;
            NonWildCardsBySuit = nonWildCardsBySuit;
            NonWildCardsBySuitAndRank = nonWildCardsBySuitAndRank;
            NonWildCardsBySuitRankLookup = nonWildCardsBySuitRankLookup;
        }

        public List<Card> Cards { get; }
        public List<Card> WildCards { get; }
        public List<Card> NonWildCards { get; }
        public Dictionary<Rank, List<Card>> AllCardsByRank { get; }
        public Dictionary<Rank, List<Card>> NonWildCardsByRank { get; }
        public Dictionary<Suit, List<Card>> NonWildCardsBySuit { get; }
        public Dictionary<Suit, Dictionary<Rank, Card>> NonWildCardsBySuitAndRank { get; }
        private Card[,] NonWildCardsBySuitRankLookup { get; }

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

            return new HandIndex(
                cardList,
                wildCards,
                nonWildCards,
                allCardsByRank,
                nonWildCardsByRank,
                nonWildCardsBySuit,
                nonWildCardsBySuitAndRank,
                nonWildCardsBySuitRankLookup);
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

        private static void AddToRankIndex(Dictionary<Rank, List<Card>> index, Card card)
        {
            if (!index.TryGetValue(card.Rank, out var cards))
            {
                cards = new List<Card>();
                index[card.Rank] = cards;
            }

            cards.Add(card);
        }
    }
}
