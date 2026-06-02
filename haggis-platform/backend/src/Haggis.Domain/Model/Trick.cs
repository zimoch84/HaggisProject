using Haggis.Domain.Extentions;
using Haggis.Domain.Enums;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Domain.Model
{
    /**
     *
     * @author Piotr Grudzień
     */

    public class Trick : IComparable, ICloneable, IEquatable<Trick>
    {
        private TrickType _type;
        private List<Card> _cards;

        public bool IsFinal;
        public TrickType Type { get => _type; set => _type = value; }
        public List<Card> Cards { get => _cards; set => _cards = value; }
        public BombType? Bomb { get => _cards.GetBombType(); }

        public Trick(TrickType type, List<Card> cards)
            : this(type, cards, copyCards: true, sortCards: true)
        {
        }

        private Trick(TrickType type, List<Card> cards, bool copyCards, bool sortCards)
        {
            Type = type;
            Cards = copyCards ? cards?.DeepCopy().ToList() : cards;

            if (sortCards)
            {
                Cards.Sort();
            }
        }

        internal static Trick FromGeneratedCards(TrickType type, List<Card> cards)
        {
            return new Trick(type, cards, copyCards: false, sortCards: false);
        }

        public Card FirstCard()
        {
            return Cards.First();
        }
        public Card LastCard()
        {
            return Cards.Last();
        }

        public int CompareTo(object obj)
        {
            var incomingTrick = (Trick)obj;
            if (Type != TrickType.BOMB && incomingTrick.Type != TrickType.BOMB) {

                int typeComparison = Type.CompareTo(incomingTrick.Type);
                if (typeComparison != 0)
                {
                    return typeComparison;
                }
 
                return FirstCard().CompareTo(incomingTrick.FirstCard());
            }
            

            if (Type != TrickType.BOMB && incomingTrick.Type == TrickType.BOMB)
                return -1;

            if (Type == TrickType.BOMB && incomingTrick.Type != TrickType.BOMB)
                return 1;

            return Bomb.Value.CompareTo(incomingTrick.Bomb.Value);

        }

        override public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (IsFinal)
                sb.Append("Final ");
            sb.Append(Type);
            sb.Append("[");

            for (int i = 0; i < Cards.Count; i++)
            {
                sb.Append(Cards[i].ToString());
                if (i < Cards.Count - 1)
                {
                    sb.Append("|"); 
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        public object Clone()
        {
            return new Trick(_type, _cards)
            {
                IsFinal = IsFinal
            };
        }

        public bool Equals(Trick other)
        {
            return _cards.SequenceEqual(other._cards);
        }
    }
}
