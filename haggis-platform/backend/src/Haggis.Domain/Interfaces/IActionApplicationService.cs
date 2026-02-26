using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface IActionApplicationService
    {
        void Apply(HaggisGameState state, HaggisAction action);
    }
}
