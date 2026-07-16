using System.Collections.Generic;
using Haggis.Domain.Model;

namespace Haggis.AI.Interfaces
{
    public interface IContinuationTrickFilterStrategy
    {
        List<Trick> FilterTricks(List<Trick> tricks, RoundState gameState);
    }
}
