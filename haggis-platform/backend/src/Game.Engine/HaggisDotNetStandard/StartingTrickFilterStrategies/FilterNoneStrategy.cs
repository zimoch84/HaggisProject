using Haggis.Interfaces;
using Haggis.Model;
using System.Collections.Generic;

namespace Haggis.StartingTrickFilterStrategies
{
    public class FilterNoneStrategy : IStartingTrickFilterStrategy
    {
        public List<Trick> FilterTricks(List<Trick> tricks)
        {
            return tricks;
        }
    }
}
