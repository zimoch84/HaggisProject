using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using static Haggis.Domain.Enums.TrickType;

namespace Haggis.Domain.Extentions
{
    public static class CardListExtention
    {
        private static readonly HashSet<TrickType> SameCardTypes = new HashSet<TrickType> { SINGLE, PAIR, TRIPLE, QUAD, FIVED, SIXED };
        private static readonly HashSet<TrickType> SequenceTypes = new HashSet<TrickType> { SEQ3, SEQ4, SEQ5, SEQ6, SEQ7 };
        private static readonly HashSet<TrickType> PairedSequenceTypes = new HashSet<TrickType> { PAIRSEQ2, PAIRSEQ3, PAIRSEQ4, PAIRSEQ5, PAIRSEQ6, PAIRSEQ7 };
        private static readonly Suit[] AllSuits = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToArray();

        public static List<Trick> FindCardSequences(this List<Card> cards, TrickType sequenceType)
        {
            return HandIndex.Build(cards).FindCardSequences(sequenceType);
        }

        public static List<Trick> FindCardSequences(this HandIndex handIndex, TrickType sequenceType)
        {
            var tricks = new List<Trick>();
            if (!SequenceTypes.Contains(sequenceType))
            {
                return tricks;
            }

            var sequenceLength = (int)((int)sequenceType - 2) / 10;

            foreach (var suit in AllSuits)
            {
                var singleSuit = handIndex.GetNonWildCardsBySuit(suit);
                if (singleSuit.Count == 0)
                {
                    continue;
                }

                for (var startRank = (int)Rank.TWO; startRank <= (int)Rank.KING - sequenceLength + 1; startRank++)
                {
                    var sequence = BuildSequenceWithWilds(
                        handIndex,
                        suit,
                        (Rank)startRank,
                        sequenceLength);

                    if (sequence.Count == sequenceLength &&
                        sequence.Any(card => !card.IsWild) &&
                        sequence.IsSequence() &&
                        !IsBomb(sequence))
                    {
                        tricks.Add(new Trick(sequenceType, sequence));
                    }
                }
            }

            return tricks;
        }

        private static List<Card> BuildSequenceWithWilds(
            HandIndex handIndex,
            Suit suit,
            Rank firstRank,
            int sequenceLength)
        {
            var sequence = new List<Card>(sequenceLength);
            var availableWilds = new Queue<Card>(handIndex.WildCards);

            for (var rankValue = (int)firstRank; rankValue < (int)firstRank + sequenceLength; rankValue++)
            {
                var rank = (Rank)rankValue;
                if (handIndex.TryGetNonWildCard(suit, rank, out var matchingCard))
                {
                    sequence.Add(matchingCard);
                    continue;
                }

                if (availableWilds.Count == 0)
                {
                    return new List<Card>();
                }

                sequence.Add(availableWilds.Dequeue().WildAs(new Card(rank, suit)));
            }

            return sequence;
        }

        public static List<Trick> FindTheSameCards(this List<Card> cards, TrickType trickType)
        {
            return HandIndex.Build(cards).FindTheSameCards(trickType);
        }

        public static List<Trick> FindTheSameCards(this HandIndex handIndex, TrickType trickType)
        {
            var tricks = new List<Trick>();
            if (!SameCardTypes.Contains(trickType))
            {
                return tricks;
            }

            var numberOfTheSameCards = (int)trickType / 10;

            foreach (var rankGroup in handIndex.AllCardsByRank.Values)
            {
                if (rankGroup.Count < numberOfTheSameCards)
                {
                    continue;
                }

                var combinations = GetKCombinationsByRankAndSuit(rankGroup, numberOfTheSameCards);
                foreach (var combination in combinations)
                {
                    tricks.Add(new Trick(trickType, combination.ToList()));
                }
            }

            return tricks;
        }

        public static List<Trick> FindTheSameCardsWithWildCards(this List<Card> cards, TrickType wildTrickType)
        {
            return HandIndex.Build(cards).FindTheSameCardsWithWildCards(wildTrickType);
        }

        public static List<Trick> FindTheSameCardsWithWildCards(this HandIndex handIndex, TrickType wildTrickType)
        {
            var wildTricks = new List<Trick>();

            if (wildTrickType == TrickType.SINGLE)
            {
                return wildTricks;
            }

            var wildCards = handIndex.WildCards;
            if (wildCards.Count == 0)
            {
                return wildTricks;
            }

            var requiredCardCount = (int)wildTrickType / 10;

            foreach (var sameRankCards in handIndex.NonWildCardsByRank.Values)
            {
                var maxNonWildCards = Math.Min(sameRankCards.Count, requiredCardCount - 1);
                if (maxNonWildCards <= 0)
                {
                    continue;
                }

                for (var nonWildCardCount = 1; nonWildCardCount <= maxNonWildCards; nonWildCardCount++)
                {
                    var requiredWildCards = requiredCardCount - nonWildCardCount;
                    if (requiredWildCards <= 0 || requiredWildCards > wildCards.Count)
                    {
                        continue;
                    }

                    var baseTricks = GetKCombinationsByRankAndSuit(sameRankCards, nonWildCardCount)
                        .Select(combination => combination.ToList())
                        .ToList();
                    var wildCombinations = GetKCombinationsByRank(wildCards, requiredWildCards)
                        .Select(combination => combination.ToList())
                        .ToList();

                    foreach (var baseTrick in baseTricks)
                    {
                        foreach (var wildCombination in wildCombinations)
                        {
                            var wildcardReplacements = wildCombination
                                .Select(wild => wild.WildAs(baseTrick.Last()))
                                .ToList();
                            var trickCards = new List<Card>(baseTrick);
                            trickCards.AddRange(wildcardReplacements);

                            wildTricks.Add(new Trick(wildTrickType, trickCards));
                        }
                    }
                }
            }

            return wildTricks
                .GroupBy(trick => trick.ToString())
                .Select(group => group.First())
                .ToList();
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
            if (sequence.Count < 4)
            {
                return false;
            }

            if (sequence.Exists(card => card.Rank == Rank.THREE) &&
                sequence.Exists(card => card.Rank == Rank.FIVE) &&
                sequence.Exists(card => card.Rank == Rank.SEVEN) &&
                sequence.Exists(card => card.Rank == Rank.NINE) &&
                (sequence.GroupBy(card => card.Suit).Count() == 4 || sequence.GroupBy(card => card.Suit).Count() == 1))
            {
                return true;
            }

            return false;
        }

        public static bool IsWildedBomb(this List<Card> sequence)
        {
            if (sequence.Count > 3 || sequence.Count < 2)
            {
                return false;
            }

            return sequence.All(card => card.IsWild);
        }

        public static IEnumerable<IEnumerable<Card>> GetKCombinationsByRankAndSuit(this List<Card> list, int length)
        {
            if (length == 1)
            {
                return list.Select(t => new Card[] { t });
            }

            return GetKCombinationsByRankAndSuit(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareBySuitAndRank(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new Card[] { t2 }));
        }

        public static IEnumerable<IEnumerable<Card>> GetKCombinationsByRank(this List<Card> list, int length)
        {
            if (length == 1)
            {
                return list.Select(t => new Card[] { t });
            }

            return GetKCombinationsByRank(list, length - 1)
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
            return HandIndex.Build(hand).FindAllPossibleBombs();
        }

        public static List<Trick> FindAllPossibleBombs(this HandIndex handIndex)
        {
            var bombs = new List<Trick>();

            var threes = handIndex.GetNonWildCardsByRank(Rank.THREE);
            var fives = handIndex.GetNonWildCardsByRank(Rank.FIVE);
            var sevens = handIndex.GetNonWildCardsByRank(Rank.SEVEN);
            var nines = handIndex.GetNonWildCardsByRank(Rank.NINE);

            foreach (var three in threes)
            {
                foreach (var five in fives)
                {
                    foreach (var seven in sevens)
                    {
                        foreach (var nine in nines)
                        {
                            var cards = new List<Card> { three, five, seven, nine };
                            if (cards.IsBomb())
                            {
                                bombs.Add(new Trick(TrickType.BOMB, cards));
                            }
                        }
                    }
                }
            }

            var wildCards = handIndex.WildCards;
            bombs.AddRange(GetKCombinationsByRank(wildCards, 2)
                .Select(combination => combination.ToList())
                .Where(cards => cards.IsBomb())
                .Select(cards => new Trick(TrickType.BOMB, cards)));

            bombs.AddRange(GetKCombinationsByRank(wildCards, 3)
                .Select(combination => combination.ToList())
                .Where(cards => cards.IsBomb())
                .Select(cards => new Trick(TrickType.BOMB, cards)));

            return bombs;
        }

        public static bool Contains(this List<Card> cards, string card)
        {
            return cards.Contains(card.ToCard());
        }

        public static List<Trick> FindPairedSequences(this List<Card> cards, TrickType sequenceType)
        {
            return HandIndex.Build(cards).FindPairedSequences(sequenceType);
        }

        public static List<Trick> FindPairedSequences(this HandIndex handIndex, TrickType sequenceType)
        {
            if (sequenceType.Class() != TrickClass.SEQUENCE_OF_PAIRS)
            {
                return null;
            }

            var pairSequenceLength = ((int)sequenceType - 4) / 20;
            var tricks = new List<Trick>();

            for (var startRank = (int)Rank.TWO; startRank <= (int)Rank.KING - pairSequenceLength + 1; startRank++)
            {
                for (var firstSuitIndex = 0; firstSuitIndex < AllSuits.Length; firstSuitIndex++)
                {
                    for (var secondSuitIndex = firstSuitIndex + 1; secondSuitIndex < AllSuits.Length; secondSuitIndex++)
                    {
                        var availableWilds = new Queue<Card>(handIndex.WildCards);
                        var firstSequence = BuildSuitedSequenceWithSharedWilds(
                            handIndex,
                            availableWilds,
                            (Rank)startRank,
                            pairSequenceLength,
                            AllSuits[firstSuitIndex]);
                        if (firstSequence.Count != pairSequenceLength)
                        {
                            continue;
                        }

                        var secondSequence = BuildSuitedSequenceWithSharedWilds(
                            handIndex,
                            availableWilds,
                            (Rank)startRank,
                            pairSequenceLength,
                            AllSuits[secondSuitIndex]);
                        if (secondSequence.Count != pairSequenceLength)
                        {
                            continue;
                        }

                        var pairedSequenceCards = new List<Card>();
                        pairedSequenceCards.AddRange(firstSequence);
                        pairedSequenceCards.AddRange(secondSequence);
                        if (pairedSequenceCards.Any(card => !card.IsWild))
                        {
                            tricks.Add(new Trick(sequenceType, pairedSequenceCards));
                        }
                    }
                }
            }

            return tricks
                .GroupBy(trick => trick.ToString())
                .Select(group => group.First())
                .ToList();
        }

        private static List<Card> BuildSuitedSequenceWithSharedWilds(
            HandIndex handIndex,
            Queue<Card> availableWilds,
            Rank firstRank,
            int sequenceLength,
            Suit suit)
        {
            var sequence = new List<Card>();

            for (var rankValue = (int)firstRank; rankValue < (int)firstRank + sequenceLength; rankValue++)
            {
                var rank = (Rank)rankValue;
                if (handIndex.TryGetNonWildCard(suit, rank, out var matchingCard))
                {
                    sequence.Add(matchingCard);
                    continue;
                }

                if (availableWilds.Count == 0)
                {
                    return new List<Card>();
                }

                sequence.Add(availableWilds.Dequeue().WildAs(new Card(rank, suit)));
            }

            return sequence;
        }
    }
}
