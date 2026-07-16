using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickFilterStrategies
{
    public sealed class FilterDistinctContinuationStrategy : IContinuationTrickFilterStrategy
    {
        public List<Trick> FilterTricks(List<Trick> tricks, RoundState gameState)
        {
            return (tricks ?? new List<Trick>())
                .GroupBy(trick => trick)
                .Select(group => group.First())
                .ToList();
        }
    }
}
