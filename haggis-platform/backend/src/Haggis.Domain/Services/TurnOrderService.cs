using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Linq;

namespace Haggis.Domain.Services
{
    public sealed class TurnOrderService : ITurnOrderService
    {
        public void Update(HaggisGameState state, HaggisAction action)
        {
            var player = state.Players.First(p => p.GUID == action.Player.GUID);
            if (player.Finished)
            {
                state.PlayerQueue.RemoveFromQueue(action.Player);
                return;
            }

            state.PlayerQueue.RotatePlayersClockwise();
        }
    }
}
