using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Linq;

namespace Haggis.Domain.Services
{
    public sealed class BoardMutationService : IBoardMutationService
    {
        public void Apply(RoundState state, HaggisAction action)
        {
            if (!action.IsPass)
            {
                var player = state.Players.First(p => p.GUID == action.Player.GUID);
                player.RemoveFromHand(action.Trick.Cards);
            }

            state.CurrentTrickPlay.AddAction(action);
        }
    }
}
