using Haggis.Domain.Model;
using System.Collections.Generic;

namespace Haggis.AI.Interfaces
{
    public interface IStartingTrickFilterStrategy
    {
        List<Trick> FilterTricks(List<Trick> tricks);
    }
}
