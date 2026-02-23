using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Domain.Extentions
{
    public static class TrickListExtention
    {
        public static bool HasContinuation(this List<Trick> tricks, Trick comparedTrick) {

            var highiestCardOfComparedTrick = comparedTrick.Cards.Last();

            var highierTricks = tricks
                .Where(trick => trick.Cards.Where(c => c.IsWild).Count() == 0)
                .Where(trick => trick.Type.Equals(comparedTrick.Type))
                .Where(trick => trick.CompareTo(comparedTrick) == 1)
                .Where(trick => !trick.Cards.Contains(highiestCardOfComparedTrick))
                ;
            
            return highierTricks.Count() > 0;
        }
        
        public static bool HasContinuationWithWilds(this List<Trick> tricks, Trick comparedTrick) {

            var highiestCardOfComparedTrick = comparedTrick.Cards.Last();

            var highierTricks = tricks
               .Where(trick => trick.Type.Equals(comparedTrick.Type))
               .Where(trick => trick.CompareTo(comparedTrick) == 1)
               .Where(trick => !trick.Cards.Contains(highiestCardOfComparedTrick))
               ;

            return highierTricks.Count() > 0;
        }
    }
}
