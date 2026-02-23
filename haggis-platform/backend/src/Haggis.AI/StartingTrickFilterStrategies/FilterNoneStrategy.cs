using Haggis.AI.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;

namespace Haggis.AI.StartingTrickFilterStrategies
{
    public class FilterNoneStrategy : IStartingTrickFilterStrategy
    {
        public List<Trick> FilterTricks(List<Trick> tricks)
        {
            return tricks;
        }
    }
}
