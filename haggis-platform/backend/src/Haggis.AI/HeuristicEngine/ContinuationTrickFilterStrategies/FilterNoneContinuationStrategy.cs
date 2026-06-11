using System.Collections.Generic;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickFilterStrategies
{
    public sealed class FilterNoneContinuationStrategy : IContinuationTrickFilterStrategy
    {
        public List<Trick> FilterTricks(List<Trick> tricks, RoundState gameState)
        {
            return tricks ?? new List<Trick>();
        }
    }
}
