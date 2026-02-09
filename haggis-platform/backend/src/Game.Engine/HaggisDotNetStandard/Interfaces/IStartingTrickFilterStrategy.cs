using Haggis.Model;
using System.Collections.Generic;

namespace Haggis.Interfaces
{
    public interface IStartingTrickFilterStrategy
    {
        List<Trick> FilterTricks(List<Trick> tricks);
    }
}
