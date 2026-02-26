using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Domain.Services
{
    public sealed class TrickResolutionService : ITrickResolutionService
    {
        public void Resolve(HaggisGameState state)
        {
            if (!state.CurrentTrickPlayState.IsEndingPass())
            {
                return;
            }

            var takingPlayer = state.CurrentTrickPlayState.Taking();
            state.ActionArchive.AddLast(state.CurrentTrickPlayState);

            var target = state.Players.First(p => p.GUID == takingPlayer.GUID);
            var cardsToMove = state.CurrentTrickPlayState.Actions
                .Where(a => !a.IsPass && a.Trick != null)
                .SelectMany(a => a.Trick.Cards)
                .ToList();

            if (cardsToMove.Count > 0)
            {
                target.AddToDiscard(new List<Card>(cardsToMove));
            }

            state.CurrentTrickPlayState.Clear();
            state.CurrentTrickPlayState = new TrickPlay(state.Players.Where(p => !p.Finished).Count());
        }
    }
}
