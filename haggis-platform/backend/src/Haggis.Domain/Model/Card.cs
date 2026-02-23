using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using System;

namespace Haggis.Domain.Model
{
    public class Card : IComparable, ICloneable, IEquatable<Card>
    {
        public Rank Rank { get => GetRank(); }
        public Rank BaseRank { get; }
        public Suit Suit { get; }
        public bool IsWild { get => Wild(); }
        public Card Replaces { get; }

        public Card(Rank rank, Suit color)
        {
            EnsureDefinedRank(rank);
            EnsureDefinedSuit(color);

            BaseRank = rank;
            Suit = color;
        }

        public Card(Rank rank)
        {
            EnsureDefinedRank(rank);
            if (!IsWildRank(rank))
                throw new ArgumentException("Only wild ranks (J/Q/K) can be created without suit.", nameof(rank));

            BaseRank = rank;
        }

        private Card(Rank rank, Card replace)
        {
            EnsureDefinedRank(rank);
            if (!IsWildRank(rank))
                throw new ArgumentException("Only wild card can replace another card.", nameof(rank));
            if (replace == null)
                throw new ArgumentNullException(nameof(replace));
            if (replace.IsWild)
                throw new ArgumentException("Wild card cannot replace another wild card.", nameof(replace));

            BaseRank = rank;
            Replaces = replace;
        }

        public Card WildAs(Card replaces)
        {
            if (!IsWild)
                throw new InvalidOperationException("Only wild cards can replace another card.");

            return new Card(BaseRank, replaces);
        }

        public Card UpRank()
        {
            if (BaseRank == Rank.KING)
                throw new InvalidOperationException("Cannot increase rank above KING.");

            var nextRank = BaseRank + 1;
            if (IsWildRank(nextRank))
                return new Card(nextRank);

            return new Card(nextRank, Suit);
        }

        override
        public string ToString()
        {
            if (IsWild)
            {
                if (Replaces == null)
                    return string.Format("{0}", Rank.ToLetter());
                else
                    return string.Format("{0}[{1}]", BaseRank.ToLetter(), (int)Rank);
            }

            if (!Rank.Equals(Rank.TEN))
                return string.Format("{0}{1}", (int)Rank, Suit.ToString().ToCharArray()[0]);
            else
                return string.Format("{0}{1}", "10", Suit.ToString().ToCharArray()[0]);
        }

        public int CompareBySuitAndRank(object obj)
        {
            if (!(obj is Card incomingCard))
                throw new ArgumentException("Object is not a Card.", nameof(obj));

            int rankComparison = Rank.CompareTo(incomingCard.Rank);
            if (rankComparison != 0)
            {
                return rankComparison;
            }

            return Suit.CompareTo(incomingCard.Suit);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Card incomingCard))
                throw new ArgumentException("Object is not a Card.", nameof(obj));

            return Rank.CompareTo(incomingCard.Rank);
        }

        public int RankDiff(Card card)
        {

            return (int)Rank - (int)card.Rank;
        }

        private bool Wild()
        {
            switch (BaseRank)
            {
                case Rank.JACK:
                case Rank.QUEEN:
                case Rank.KING: return true;
                default: return false;
            }
        }
        private Rank GetRank()
        {

            if (!(Wild() && Replaces != null))
                return BaseRank;

            return Replaces.Rank;
        }
        public object Clone()
        {
            if (Replaces != null)
                return new Card(BaseRank, (Card)Replaces.Clone());

            if (IsWildRank(BaseRank))
                return new Card(BaseRank);

            return new Card(BaseRank, Suit);
        }

        public bool Equals(Card other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null)
                return false;
            return BaseRank == other.BaseRank &&
                   Suit == other.Suit;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Card);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + BaseRank.GetHashCode();
                hash = hash * 23 + Suit.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Card left, Card right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Card left, Card right)
        {
            return !Equals(left, right);
        }

        private static bool IsWildRank(Rank rank)
        {
            return rank == Rank.JACK || rank == Rank.QUEEN || rank == Rank.KING;
        }

        private static void EnsureDefinedRank(Rank rank)
        {
            if (!Enum.IsDefined(typeof(Rank), rank))
                throw new ArgumentOutOfRangeException(nameof(rank), "Rank has invalid value.");
        }

        private static void EnsureDefinedSuit(Suit suit)
        {
            if (!Enum.IsDefined(typeof(Suit), suit))
                throw new ArgumentOutOfRangeException(nameof(suit), "Suit has invalid value.");
        }
    }
}
