using Haggis.Domain.Extentions;
using Haggis.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haggis.Domain.Model
{
    /**
     *
     * @author Piotr GrudzieĹ„
     */

    public class Trick : IComparable, ICloneable, IEquatable<Trick>
    {
        private readonly TrickType _type;
        private readonly List<Card> _cards;
        private readonly BombType? _bomb;

        public TrickType Type => _type;
        public IReadOnlyList<Card> Cards => _cards;
        public BombType? Bomb => _bomb;

        public Trick(TrickType type, List<Card> cards)
            : this(type, cards, sortCards: true)
        {
        }

        private Trick(TrickType type, List<Card> cards, bool sortCards)
        {
            _type = type;
            _cards = cards == null ? new List<Card>() : new List<Card>(cards);

            if (sortCards)
            {
                _cards.Sort();
            }

            _bomb = _cards.GetBombType();
        }

        internal static Trick FromGeneratedCards(TrickType type, List<Card> cards)
        {
            return new Trick(type, cards, sortCards: false);
        }

        public Card FirstCard()
        {
            return _cards[0];
        }

        public Card LastCard()
        {
            return _cards[_cards.Count - 1];
        }

        public int CompareTo(object obj)
        {
            var incomingTrick = (Trick)obj;
            var thisType = Type;
            var incomingType = incomingTrick.Type;
            var thisIsBomb = thisType == TrickType.BOMB;
            var incomingIsBomb = incomingType == TrickType.BOMB;

            if (!thisIsBomb && !incomingIsBomb)
            {
                var typeComparison = ((int)thisType).CompareTo((int)incomingType);
                if (typeComparison != 0)
                {
                    return typeComparison;
                }

                return _cards[0].CompareTo(incomingTrick._cards[0]);
            }

            if (!thisIsBomb && incomingIsBomb)
            {
                return -1;
            }

            if (thisIsBomb && !incomingIsBomb)
            {
                return 1;
            }

            return ((int)Bomb.Value).CompareTo((int)incomingTrick.Bomb.Value);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_type);
            sb.Append("[");

            for (int i = 0; i < _cards.Count; i++)
            {
                sb.Append(_cards[i].ToString());
                if (i < _cards.Count - 1)
                {
                    sb.Append("|");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }

        public object Clone()
        {
            return this;
        }

        public bool Equals(Trick other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || _type != other._type || _cards.Count != other._cards.Count)
            {
                return false;
            }

            for (var index = 0; index < _cards.Count; index++)
            {
                if (!_cards[index].Equals(other._cards[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Trick);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + _type.GetHashCode();

                for (var index = 0; index < _cards.Count; index++)
                {
                    hash = hash * 23 + _cards[index].GetHashCode();
                }

                return hash;
            }
        }
    }
}
