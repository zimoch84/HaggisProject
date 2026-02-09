using Haggis.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Extentions
{
    public static class CardsExtensions
    {
        public static List<Card> Cards(params string[] cardStrings)
        {
            return cardStrings.Select(s => s.ToCard()).ToList();

        }
    }
}