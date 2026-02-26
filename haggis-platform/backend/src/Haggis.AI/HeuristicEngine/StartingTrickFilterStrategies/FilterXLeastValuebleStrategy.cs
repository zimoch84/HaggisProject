using Haggis.AI.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.AI.StartingTrickFilterStrategies
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
