using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface ITurnOrderService
    {
        void Update(RoundState state, HaggisAction action);
    }
}
