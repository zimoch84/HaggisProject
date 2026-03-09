using Haggis.Domain.Model;
using System.Collections.Generic;

namespace Haggis.Domain.Interfaces
{
    public interface ITrickGenerationService
    {
        List<Trick> GetPossibleOpeningTricks(IHaggisPlayer player);
        List<Trick> GetPossibleContinuationTricks(IHaggisPlayer player, Trick lastTrick);
    }
}
