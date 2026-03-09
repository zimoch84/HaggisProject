using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.AI.StartingTrickFilterStrategies
{
    public class FilterContinuations : IStartingTrickFilterStrategy
    {

        private int _minNumberOfTricks;
        private bool _useWildsInContinuations;

        public FilterContinuations(int minNumberOfTricks, bool useWildsInContinuations)
        {
            _minNumberOfTricks = minNumberOfTricks;
            _useWildsInContinuations = useWildsInContinuations;
        }

        public List<Trick> FilterTricks(List<Trick> tricks)
        {

            if (tricks.Count <= _minNumberOfTricks)
                return tricks;

            IEnumerable<Trick> filteredTricks;

            if (_useWildsInContinuations)
            {
                filteredTricks = tricks.Where(trick => trick.Type != TrickType.SINGLE)
                                       .Where(trick => tricks.HasContinuationWithWilds(trick));
            }
            else
            {
                filteredTricks = tricks.Where(trick => trick.Type != TrickType.SINGLE)
                                       .Where(trick => trick.Cards.Where(c => c.IsWild).Count() == 0)
                                       .Where(trick => tricks.HasContinuation(trick));
            }

            if (filteredTricks.Any())
                return filteredTricks.ToList(); ;

            return tricks;
        }
    }
}
