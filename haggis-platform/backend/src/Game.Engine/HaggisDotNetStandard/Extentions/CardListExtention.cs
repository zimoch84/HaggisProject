using Haggis.Enums;
using Haggis.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using static Haggis.Enums.TrickType;


namespace Haggis.Extentions
{
    public static class CardListExtention
    {
        public static List<Trick> FindCardSequences(this List<Card> cards, TrickType sequenceType)
        {
            var tricks = new List<Trick>();
            var sequenceLength = (int)((int)sequenceType - 2) / 10;
            cards.Sort();
            var groupedCards = cards.GroupBy(card => card.Suit);

            foreach (var suitArray in groupedCards)
            {
                var singleSuit = suitArray.ToList();
                for (int i = 0; i < singleSuit.Count() - sequenceLength + 1; i++)
                {
                    var sequence = singleSuit.GetRange(i, sequenceLength);

                    if (IsSequence(sequence) && !IsBomb(sequence))
                    {
                        tricks.Add(new Trick(sequenceType, sequence));
                    }
                }
            }
            return tricks;
        }
        public static List<Trick> FindTheSameCards(this List<Card> cards, TrickType trickType)
        {
            var tricks = new List<Trick>();
            var sameCardTypes = new List<TrickType>() { SINGLE, PAIR, TRIPLE, QUAD, FIVED, SIXED };
            if (!sameCardTypes.Contains(trickType))
                return tricks;

            var numberOfTheSameCards = (int)trickType / 10;
            var groupedCards = cards.GroupBy(card => card.Rank);

            foreach (var rankArray in groupedCards)
            {
                var singleRank = rankArray.ToArray();
                if (singleRank.Count() >= numberOfTheSameCards)
                {
                    var combinations = GetKCombinationsByRankAndSuit(singleRank.ToList(), numberOfTheSameCards);
                    foreach (var combination in combinations)
                    {
                        tricks.Add(new Trick(trickType, combination.ToList()));
                    }
                }
            }
            return tricks;
        }
        public static List<Trick> FindTheSameCardsWithWildCards(this List<Card> cards, TrickType wildTrickType)
        {
            List<Trick> wildTricks = new List<Trick>();

            if (wildTrickType == TrickType.SINGLE)
                return wildTricks;

            // Use all available wild cards instead of only the first one
            var wildCards = cards.Where(c => c.IsWild).ToList();
            if (wildCards.Count == 0)
                return wildTricks;

            // Build base tricks from non-wild cards with a lesser trick type
            var baseTricks = FindTheSameCards(cards.Where(c => !c.IsWild).ToList(), wildTrickType.LesserTrick());

            foreach (var baseTrick in baseTricks)
            {
                foreach (var wild in wildCards)
                {
                    var wildCardsList = new List<Card>(baseTrick.Cards);
                    wildCardsList.Add(wild.WildAs(baseTrick.LastCard()));

                    var wildTrick = new Trick(wildTrickType, wildCardsList);
                    wildTricks.Add(wildTrick);
                }
            }
            return wildTricks;
        }

        public static bool IsSequence(this List<Card> sequence)
        {
            for (int j = 1; j < sequence.Count; j++)
            {
                if ((int)sequence[j - 1].Rank + 1 != (int)sequence[j].Rank)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsBomb(this List<Card> sequence)
        {

            return IsWildedBomb(sequence) || IsNotWildedBomb(sequence);
        }

        public static bool IsNotWildedBomb(this List<Card> sequence)
        {
            if (sequence.Count() < 4)
                return false;

            if (sequence.Exists(card => card.Rank == Rank.THREE) &&
                sequence.Exists(card => card.Rank == Rank.FIVE) &&
                sequence.Exists(card => card.Rank == Rank.SEVEN) &&
                sequence.Exists(card => card.Rank == Rank.NINE) &&
                (
                 sequence.GroupBy(card => card.Suit).Count() == 4 || sequence.GroupBy(card => card.Suit).Count() == 1
                )
                )

            {
                return true;
            }
            return false;
        }

        public static bool IsWildedBomb(this List<Card> sequence)
        {

            if (sequence.Count() > 3 || sequence.Count < 2)
                return false;
            return sequence.All(card => card.IsWild);
        }

        public static IEnumerable<IEnumerable<Card>> GetKCombinationsByRankAndSuit(this List<Card> list, int length)
        {
            if (length == 1) return list.Select(t => new Card[] { t });
            return GetKCombinationsByRankAndSuit(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareBySuitAndRank(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new Card[] { t2 }));
        }

        public static IEnumerable<IEnumerable<Card>> GetKCombinationsByRank(this List<Card> list, int length)
        {
            if (length == 1) return list.Select(t => new Card[] { t });
            return GetKCombinationsByRankAndSuit(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareBySuitAndRank(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new Card[] { t2 }));
        }

        public static void Shuffle(this List<Card> deck)
        {
            var r = new Random();
            for (int n = deck.Count - 1; n > 0; --n)
            {
                int k = r.Next(n + 1);
                Card temp = deck[n];
                deck[n] = deck[k];
                deck[k] = temp;
            }
        }

        public static List<Trick> FindAllPossibleBombs(this List<Card> hand)
        {
            List<Trick> bombs = new List<Trick>();
            var filteredCards = hand.Where(card => card.Rank == Rank.THREE || card.Rank == Rank.FIVE ||
                                    card.Rank == Rank.SEVEN || card.Rank == Rank.NINE).ToList();
            var combinations = filteredCards.GetKCombinationsByRank(4);
            foreach (var combination in combinations)
            {
                if (combination.ToList().IsBomb())
                    bombs.Add(new Trick(TrickType.BOMB, combination.ToList()));
            }
            var wildedCards = hand.Where(card => card.IsWild).ToList();
            var wildedCombination = wildedCards.GetKCombinationsByRank(2);
            foreach (var combination in wildedCombination)
            {
                if (combination.ToList().IsBomb())
                    bombs.Add(new Trick(TrickType.BOMB, combination.ToList()));
            }

            wildedCombination = wildedCards.GetKCombinationsByRank(3);
            foreach (var combination in wildedCombination)
            {
                if (combination.ToList().IsBomb())
                    bombs.Add(new Trick(TrickType.BOMB, combination.ToList()));
            }
            return bombs;
        }
        public static bool Contains(this List<Card> cards, string card)
        {

            return cards.Contains(card.ToCard());
        }

        public static List<Trick> FindPairedSequences(this List<Card> cards, TrickType sequenceType)
        {
            if (sequenceType.Class() != TrickClass.SEQUENCE_OF_PAIRS)
                return null;
            
            var tricks = new List<Trick>();
            var pairedType = sequenceType.SeqByPair(); // Uzyskaj odpowiedni typ pary na podstawie sekwencji
            var groupedCards = cards.GroupBy(card => card.Suit);

            // Znajdź wszystkie sekwencje na podstawie podanego typu
            var allSequences = groupedCards
                .SelectMany(group => group.ToList().FindCardSequences(pairedType))
                .ToList();

            // Zwróć tricki typu PAIR na podstawie zgrupowanych sekwencji
            var allPairedSequences = allSequences
                .GroupBy(seq => seq.Cards.Min(card => card.Rank)) // Grupuj według minimalnej karty
                .Where(group => group.Count() >= 2) // Wybierz tylko grupy z co najmniej dwiema sekwencjami
                .Select(group => new Trick(sequenceType, group.SelectMany(seq => seq.Cards).ToList())) // Twórz nowe tricki PAIR
                .ToList();

            return allPairedSequences;
        }
    }
}

