using Haggis.AI.Interfaces;
using Haggis.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.AI.StartingTrickFilterStrategies
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
