using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Domain.Extentions
{
    public static class StringArrayExtensions
    {
        public static Trick[] ToTricks(this string[] trickStrings)
        {
            return trickStrings.Select(trickString => trickString.ToTrick()).ToArray();
        }

        public static List<Card> ToCards(this string[] cardStrings)
        {
            List<Card> cards = new List<Card>();
            cardStrings.ToList().ForEach(c => cards.Add(c.ToCard()));

            return cards;
        }
    }
}
