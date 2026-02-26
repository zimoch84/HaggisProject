using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Linq;

namespace Haggis.Domain.Services
{
    public sealed class BoardMutationService : IBoardMutationService
    {
        public void Apply(HaggisGameState state, HaggisAction action)
        {
            if (!action.IsPass)
            {
                var player = state.Players.First(p => p.GUID == action.Player.GUID);
                player.RemoveFromHand(action.Trick.Cards);
            }

            state.CurrentTrickPlayState.AddAction(action);
        }
    }
}
