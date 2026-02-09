using Haggis.Interfaces;
using Haggis.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.StartingTrickFilterStrategies
{
    public class FilterXLeastValuebleStrategy : IStartingTrickFilterStrategy
    {
        private int _maxValue;

        public FilterXLeastValuebleStrategy(int maxValue)
        {
            _maxValue = maxValue;
        }

        public List<Trick> FilterTricks(List<Trick> tricks)
        {
            if (tricks.Count <= _maxValue)
                return tricks;

            tricks.Sort();
            return tricks.Take(_maxValue).ToList();
        }
    }
}
