using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using System;

namespace Haggis.Domain.Model
{
    public class Card : IComparable, ICloneable, IEquatable<Card>
    {
        private Rank _rank;
        private Suit _suit;
        private Card _replaces;

        public Rank Rank { get => GetRank(); }
        public Suit Suit { get => _suit; }
        public int Point { get => PointFromRank(); }
        public bool IsWild { get => Wild(); }

        public Card(Rank rank, Suit color)
        {
            _rank = rank;
            _suit = color;
        }

        public Card(Rank rank)
        {
            _rank = rank;
        }
        private Card(Rank rank, Card replace)
        {
            _rank = rank;
            _replaces = replace;
        }
        public Card WildAs(Card replaces)
        {
            return new Card(_rank, replaces);
        }

        public Card UpRank()
        {
            if (_rank >= Rank.NINE)
                return new Card(_rank + 1);

            return new Card(_rank + 1, _suit);
        }
        override
        public string ToString()
        {
            if (IsWild)
            {
                if (_replaces == null)
                    return string.Format("{0}", Rank.ToLetter());
                else
                    return string.Format("{0}[{1}]", _rank.ToLetter(), (int)Rank);
            }

            if (!Rank.Equals(Rank.TEN))
                return string.Format("{0}{1}", (int)Rank, Suit.ToString().ToCharArray()[0]);
            else
                return string.Format("{0}{1}", "10", Suit.ToString().ToCharArray()[0]);
        }

        public int CompareBySuitAndRank(object obj)
        {
            Card incomingCard = obj as Card;

            int rankComparison = Rank.CompareTo(incomingCard.Rank);
            if (rankComparison != 0)
            {
                return rankComparison;
            }

            return Suit.CompareTo(incomingCard.Suit);
        }

        public int CompareTo(object obj)
        {
            Card incomingCard = obj as Card;

            return Rank.CompareTo(incomingCard.Rank);
        }

        public int RankDiff(Card card)
        {

            return (int)Rank - (int)card.Rank;
        }

        private int PointFromRank()
        {
            switch (_rank)
            {
                case Rank.THREE:
                case Rank.FIVE:
                case Rank.SEVEN:
                case Rank.NINE: return 1;
                case Rank.JACK: return 2;
                case Rank.QUEEN: return 3;
                case Rank.KING: return 5;
                default: return 0;
            }
        }

        private bool Wild()
        {
            switch (_rank)
            {
                case Rank.JACK:
                case Rank.QUEEN:
                case Rank.KING: return true;
                default: return false;
            }
        }
        private Rank GetRank()
        {

            if (!(Wild() && _replaces != null))
                return _rank;

            return _replaces.Rank;
        }
        public object Clone()
        {
            return new Card(_rank, _suit)
            {
                _replaces = _replaces?.Clone() as Card
            };
        }

        public bool Equals(Card other)
        {
            return _rank == other._rank && _suit == other._suit;
        }
    }
}
