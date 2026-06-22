using System.Collections.Generic;
using Haggis.AI.Interfaces;
using Haggis.AI.TrickFilters;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickFilterStrategies
{
    public sealed class FilterRedundantWildAssignmentsContinuationStrategy : IContinuationTrickFilterStrategy
    {
        public List<Trick> FilterTricks(List<Trick> tricks, RoundState gameState)
        {
            return RedundantWildAssignmentTrickFilter.Filter(tricks);
        }
    }
}
