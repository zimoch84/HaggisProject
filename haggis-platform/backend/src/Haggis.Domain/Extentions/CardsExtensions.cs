using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Domain.Extentions
{
    public static class CardsExtensions
    {
        public static List<Card> Cards(params string[] cardStrings)
        {
            return cardStrings.Select(s => s.ToCard()).ToList();

        }
    }
}