using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;

namespace Haggis.AI.TrickFilters
{
    internal static class RedundantWildAssignmentTrickFilter
    {
        public static List<Trick> Filter(List<Trick> tricks)
        {
            return (tricks ?? new List<Trick>())
                .GroupBy(GetComparableKey)
                .SelectMany(FilterComparableGroup)
                .ToList();
        }

        private static ComparableTrickKey GetComparableKey(Trick trick)
        {
            var includeEffectiveSuit = ShouldIncludeEffectiveSuit(trick?.Type ?? TrickType.PASS);
            var effectiveCards = trick?.Cards?
                .Select(card => new EffectiveCardKey(
                    (int)card.Rank,
                    includeEffectiveSuit ? GetEffectiveSuit(card) : int.MinValue))
                .OrderBy(card => card.Rank)
                .ThenBy(card => card.Suit)
                .ToArray()
                ?? Array.Empty<EffectiveCardKey>();

            return new ComparableTrickKey(trick?.Type ?? TrickType.PASS, effectiveCards);
        }

        private static bool ShouldIncludeEffectiveSuit(TrickType trickType)
        {
            switch (trickType.Class())
            {
                case TrickClass.SEQUENCE:
                case TrickClass.SEQUENCE_OF_PAIRS:
                case TrickClass.SEQUENCE_OF_TRIPLES:
                case TrickClass.SEQUENCE_OF_QUADRUPLES:
                case TrickClass.SEQUENCE_OF_FIVED:
                    return true;
                default:
                    return false;
            }
        }

        private static int GetEffectiveSuit(Card card)
        {
            if (card == null)
            {
                return int.MinValue;
            }

            if (card.Replaces != null)
            {
                return (int)card.Replaces.Suit;
            }

            return card.IsWild ? int.MinValue : (int)card.Suit;
        }

        private static IEnumerable<Trick> FilterComparableGroup(IGrouping<ComparableTrickKey, Trick> group)
        {
            var candidates = group.ToList();
            var minimumWildCount = candidates.Min(CountWildCards);
            var minimizedByWildCount = candidates
                .Where(trick => CountWildCards(trick) == minimumWildCount)
                .ToList();

            if (minimumWildCount == 0)
            {
                return minimizedByWildCount;
            }

            var bestWildRanks = minimizedByWildCount
                .Select(GetWildBaseRanks)
                .OrderBy(ranks => ranks, WildRanksComparer.Instance)
                .First();

            var minimizedByWildBaseRanks = minimizedByWildCount
                .Where(trick => WildRanksComparer.Instance.Compare(GetWildBaseRanks(trick), bestWildRanks) == 0)
                .ToList();

            var bestReplacementAssignments = minimizedByWildBaseRanks
                .Select(GetWildReplacementAssignments)
                .OrderBy(assignments => assignments, WildReplacementAssignmentsComparer.Instance)
                .First();

            return minimizedByWildBaseRanks
                .Where(trick => WildReplacementAssignmentsComparer.Instance.Compare(GetWildReplacementAssignments(trick), bestReplacementAssignments) == 0)
                .ToList();
        }

        private static int CountWildCards(Trick trick)
        {
            return trick.Cards.Count(card => card.IsWild);
        }

        private static IReadOnlyList<int> GetWildBaseRanks(Trick trick)
        {
            return trick.Cards
                .Where(card => card.IsWild)
                .Select(card => (int)card.BaseRank)
                .OrderBy(rank => rank)
                .ToList();
        }

        private static IReadOnlyList<(int ReplacementRank, int WildBaseRank)> GetWildReplacementAssignments(Trick trick)
        {
            return trick.Cards
                .Where(card => card.IsWild && card.Replaces != null)
                .Select(card => (ReplacementRank: (int)card.Replaces.Rank, WildBaseRank: (int)card.BaseRank))
                .OrderBy(assignment => assignment.ReplacementRank)
                .ThenBy(assignment => assignment.WildBaseRank)
                .ToList();
        }

        private sealed class WildRanksComparer : IComparer<IReadOnlyList<int>>
        {
            public static WildRanksComparer Instance { get; } = new WildRanksComparer();

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

                var length = x.Count < y.Count ? x.Count : y.Count;
                for (var index = 0; index < length; index++)
                {
                    if (x[index] != y[index])
                    {
                        return x[index].CompareTo(y[index]);
                    }
                }

                return x.Count.CompareTo(y.Count);
            }
        }

        private sealed class WildReplacementAssignmentsComparer : IComparer<IReadOnlyList<(int ReplacementRank, int WildBaseRank)>>
        {
            public static WildReplacementAssignmentsComparer Instance { get; } = new WildReplacementAssignmentsComparer();

            public int Compare(IReadOnlyList<(int ReplacementRank, int WildBaseRank)> x, IReadOnlyList<(int ReplacementRank, int WildBaseRank)> y)
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

                var length = x.Count < y.Count ? x.Count : y.Count;
                for (var index = 0; index < length; index++)
                {
                    if (x[index].ReplacementRank != y[index].ReplacementRank)
                    {
                        return x[index].ReplacementRank.CompareTo(y[index].ReplacementRank);
                    }

                    if (x[index].WildBaseRank != y[index].WildBaseRank)
                    {
                        return x[index].WildBaseRank.CompareTo(y[index].WildBaseRank);
                    }
                }

                return x.Count.CompareTo(y.Count);
            }
        }

        private sealed class ComparableTrickKey : IEquatable<ComparableTrickKey>
        {
            private readonly EffectiveCardKey[] _effectiveCards;

            public ComparableTrickKey(TrickType trickType, EffectiveCardKey[] effectiveCards)
            {
                TrickType = trickType;
                _effectiveCards = effectiveCards ?? Array.Empty<EffectiveCardKey>();
            }

            private TrickType TrickType { get; }

            public bool Equals(ComparableTrickKey other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (other is null ||
                    TrickType != other.TrickType ||
                    _effectiveCards.Length != other._effectiveCards.Length)
                {
                    return false;
                }

                for (var index = 0; index < _effectiveCards.Length; index++)
                {
                    if (_effectiveCards[index].Rank != other._effectiveCards[index].Rank ||
                        _effectiveCards[index].Suit != other._effectiveCards[index].Suit)
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as ComparableTrickKey);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 23 + TrickType.GetHashCode();

                    for (var index = 0; index < _effectiveCards.Length; index++)
                    {
                        hash = hash * 23 + _effectiveCards[index].Rank;
                        hash = hash * 23 + _effectiveCards[index].Suit;
                    }

                    return hash;
                }
            }
        }

        private readonly struct EffectiveCardKey
        {
            public EffectiveCardKey(int rank, int suit)
            {
                Rank = rank;
                Suit = suit;
            }

            public int Rank { get; }
            public int Suit { get; }
        }
    }
}
