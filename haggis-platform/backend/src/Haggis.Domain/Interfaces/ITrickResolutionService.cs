using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface ITrickResolutionService
    {
        void Resolve(HaggisGameState state);
    }
}
