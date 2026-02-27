using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface ITrickResolutionService
    {
        void Resolve(RoundState state);
    }
}
