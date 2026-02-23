using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Domain.Extentions
{
    public static class BombTypeExtention
    {
        private static readonly Card JACK = new Card(Rank.JACK);
        private static readonly Card QUEEN = new Card(Rank.QUEEN);
        private static readonly Card KING = new Card(Rank.KING);

        public static BombType? GetBombType(this List<Card> cards)
        {

            if (!cards.IsBomb())
                return null;

            if (cards.IsNotWildedBomb())
            {
                if (cards.GroupBy(card => card.Suit).Count() == 4)
                    return BombType.RAINBOW;

                if (cards.GroupBy(card => card.Suit).Count() == 1)
                    return BombType.COLOR;
            }

            if (cards.Count == 2)
            {
                if (cards.Contains(JACK) && cards.Contains(QUEEN))
                    return BombType.JQ;
                if (cards.Contains(JACK) && cards.Contains(KING))
                    return BombType.JK;
                if (cards.Contains(QUEEN) && cards.Contains(KING))
                    return BombType.QK;
            }
            return BombType.JQK;
        }
    }
}
