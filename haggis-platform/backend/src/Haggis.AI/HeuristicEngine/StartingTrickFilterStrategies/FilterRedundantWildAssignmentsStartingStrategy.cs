using System.Collections.Generic;
using Haggis.AI.Interfaces;
using Haggis.AI.TrickFilters;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickFilterStrategies
{
    public sealed class FilterRedundantWildAssignmentsStartingStrategy : IStartingTrickFilterStrategy
    {
        public List<Trick> FilterTricks(List<Trick> tricks)
        {
            return RedundantWildAssignmentTrickFilter.Filter(tricks);
        }
    }
}
