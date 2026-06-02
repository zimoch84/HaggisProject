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
        private static readonly HashSet<TrickType> StairTypes = new HashSet<TrickType>
        {
            PAIRSEQ2, PAIRSEQ3, PAIRSEQ4, PAIRSEQ5, PAIRSEQ6, PAIRSEQ7,
            TRIPLESTAIR2, TRIPLESTAIR3, TRIPLESTAIR4, TRIPLESTAIR5,
            QUADSTAIR2, QUADSTAIR3, QUADSTAIR4,
            FIVEDSTAIR2, FIVEDSTAIR3
        };
        private static readonly Suit[] AllSuits = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToArray();

        public static List<Trick> FindCardSequences(this List<Card> cards, TrickType sequenceType)
        {
            return HandIndex.Build(cards).FindCardSequences(sequenceType);
        }

        public static List<Trick> FindCardSequences(this HandIndex handIndex, TrickType sequenceType)
        {
            if (!SequenceTypes.Contains(sequenceType))
            {
                return new List<Trick>();
            }

            return handIndex.FindAllCardSequences()
                .Where(trick => trick.Type == sequenceType)
                .ToList();
        }

        public static List<Trick> FindAllCardSequences(this HandIndex handIndex)
        {
            var tricks = new List<Trick>();

            foreach (var suit in AllSuits)
            {
                var singleSuit = handIndex.GetNonWildCardsBySuit(suit);
                if (singleSuit.Count == 0)
                {
                    continue;
                }

                for (var startRank = (int)Rank.TWO; startRank <= (int)Rank.KING - 2; startRank++)
                {
                    for (var sequenceLength = 3; sequenceLength <= 7 && startRank + sequenceLength - 1 <= (int)Rank.KING; sequenceLength++)
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
                            var trickType = (TrickType)(sequenceLength * 10 + 2);
                            tricks.Add(new Trick(trickType, sequence));
                            continue;
                        }

                        break;
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

        public static List<Trick> FindStairs(this List<Card> cards, TrickType stairType)
        {
            return HandIndex.Build(cards).FindStairs(stairType);
        }

        public static List<Trick> FindPairedSequences(this List<Card> cards, TrickType sequenceType)
        {
            return HandIndex.Build(cards).FindStairs(sequenceType);
        }

        public static List<Trick> FindStairs(this HandIndex handIndex, TrickType stairType)
        {
            if (!StairTypes.Contains(stairType))
            {
                return new List<Trick>();
            }

            return handIndex.FindAllStairs()
                .Where(trick => trick.Type == stairType)
                .ToList();
        }

        public static List<Trick> FindAllStairs(this HandIndex handIndex)
        {
            var tricks = new List<Trick>();
            var seenTricks = new HashSet<string>();

            for (var groupSize = 2; groupSize <= 5; groupSize++)
            {
                var maxLength = GetMaxStairLength(groupSize);
                if (maxLength < 2)
                {
                    continue;
                }

                for (var startRank = (int)Rank.TWO; startRank <= (int)Rank.KING - 1; startRank++)
                {
                    var upperLength = Math.Min(maxLength, (int)Rank.KING - startRank + 1);
                    for (var stairLength = 2; stairLength <= upperLength; stairLength++)
                    {
                        if (!TryGetStairType(groupSize, stairLength, out var stairType))
                        {
                            continue;
                        }

                        foreach (var selectedSuits in GetSuitCombinations(groupSize))
                        {
                            var availableWilds = new Queue<Card>(handIndex.WildCards);
                            var stairCards = new List<Card>();
                            var succeeded = true;

                            foreach (var suit in selectedSuits)
                            {
                                var sequence = BuildSuitedSequenceWithSharedWilds(
                                    handIndex,
                                    availableWilds,
                                    (Rank)startRank,
                                    stairLength,
                                    suit);
                                if (sequence.Count != stairLength)
                                {
                                    succeeded = false;
                                    break;
                                }

                                stairCards.AddRange(sequence);
                            }

                            if (!succeeded || !stairCards.Any(card => !card.IsWild))
                            {
                                continue;
                            }

                            var trick = new Trick(stairType, stairCards);
                            if (seenTricks.Add(trick.ToString()))
                            {
                                tricks.Add(trick);
                            }
                        }
                    }
                }
            }

            return tricks;
        }

        public static List<Trick> FindPairedSequences(this HandIndex handIndex, TrickType sequenceType)
        {
            return FindStairs(handIndex, sequenceType);
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

        private static IEnumerable<IReadOnlyList<Suit>> GetSuitCombinations(int length)
        {
            var combination = new Suit[length];

            foreach (var result in GetSuitCombinationsRecursive(0, 0, length, combination))
            {
                yield return result;
            }
        }

        private static IEnumerable<IReadOnlyList<Suit>> GetSuitCombinationsRecursive(int startIndex, int depth, int length, Suit[] combination)
        {
            if (depth == length)
            {
                var result = new Suit[length];
                Array.Copy(combination, result, length);
                yield return result;
                yield break;
            }

            for (var index = startIndex; index <= AllSuits.Length - (length - depth); index++)
            {
                combination[depth] = AllSuits[index];
                foreach (var result in GetSuitCombinationsRecursive(index + 1, depth + 1, length, combination))
                {
                    yield return result;
                }
            }
        }

        private static int GetMaxStairLength(int groupSize)
        {
            switch (groupSize)
            {
                case 2:
                    return 7;
                case 3:
                    return 5;
                case 4:
                    return 4;
                case 5:
                    return 3;
                default:
                    return 0;
            }
        }

        private static bool TryGetStairType(int groupSize, int stairLength, out TrickType stairType)
        {
            stairType = default;

            switch (groupSize)
            {
                case 2:
                    switch (stairLength)
                    {
                        case 2: stairType = PAIRSEQ2; return true;
                        case 3: stairType = PAIRSEQ3; return true;
                        case 4: stairType = PAIRSEQ4; return true;
                        case 5: stairType = PAIRSEQ5; return true;
                        case 6: stairType = PAIRSEQ6; return true;
                        case 7: stairType = PAIRSEQ7; return true;
                    }
                    break;
                case 3:
                    switch (stairLength)
                    {
                        case 2: stairType = TRIPLESTAIR2; return true;
                        case 3: stairType = TRIPLESTAIR3; return true;
                        case 4: stairType = TRIPLESTAIR4; return true;
                        case 5: stairType = TRIPLESTAIR5; return true;
                    }
                    break;
                case 4:
                    switch (stairLength)
                    {
                        case 2: stairType = QUADSTAIR2; return true;
                        case 3: stairType = QUADSTAIR3; return true;
                        case 4: stairType = QUADSTAIR4; return true;
                    }
                    break;
                case 5:
                    switch (stairLength)
                    {
                        case 2: stairType = FIVEDSTAIR2; return true;
                        case 3: stairType = FIVEDSTAIR3; return true;
                    }
                    break;
            }

            return false;
        }
    }
}
