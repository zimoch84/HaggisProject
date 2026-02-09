using Haggis.Interfaces;
using Haggis.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.StartingTrickFilterStrategies
{
    public class FilterXMostValuebleStrategy : IStartingTrickFilterStrategy
    {
        private int _maxLimit;
        public FilterXMostValuebleStrategy(int maxLimit)
        {

            _maxLimit = maxLimit;
        }

        public List<Trick> FilterTricks(List<Trick> tricks)
        {
            if (tricks.Count <= _maxLimit)
                return tricks;

            tricks.Sort();
            return tricks.Skip(Math.Max(0, tricks.Count - _maxLimit)).ToList();
        }
    }
}
