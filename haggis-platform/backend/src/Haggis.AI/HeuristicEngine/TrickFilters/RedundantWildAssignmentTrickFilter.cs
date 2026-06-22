using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.Domain.Enums;
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
            var effectiveRanks = trick?.Cards?
                .Select(card => (int)card.Rank)
                .OrderBy(rank => rank)
                .ToArray()
                ?? new int[0];

            return new ComparableTrickKey(trick?.Type ?? TrickType.PASS, effectiveRanks);
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

            return minimizedByWildCount
                .Where(trick => WildRanksComparer.Instance.Compare(GetWildBaseRanks(trick), bestWildRanks) == 0)
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

        private sealed class ComparableTrickKey : IEquatable<ComparableTrickKey>
        {
            private readonly int[] _effectiveRanks;

            public ComparableTrickKey(TrickType trickType, int[] effectiveRanks)
            {
                TrickType = trickType;
                _effectiveRanks = effectiveRanks ?? new int[0];
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
                    _effectiveRanks.Length != other._effectiveRanks.Length)
                {
                    return false;
                }

                for (var index = 0; index < _effectiveRanks.Length; index++)
                {
                    if (_effectiveRanks[index] != other._effectiveRanks[index])
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

                    for (var index = 0; index < _effectiveRanks.Length; index++)
                    {
                        hash = hash * 23 + _effectiveRanks[index];
                    }

                    return hash;
                }
            }
        }
    }
}
