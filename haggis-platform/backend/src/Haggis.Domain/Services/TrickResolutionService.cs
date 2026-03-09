using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Domain.Services
{
    public sealed class TrickResolutionService : ITrickResolutionService
    {
        public void Resolve(RoundState state)
        {
            if (!state.CurrentTrickPlay.IsEndingPass())
            {
                return;
            }

            var takingPlayer = state.CurrentTrickPlay.Taking();
            state.ActionArchive.AddLast(state.CurrentTrickPlay);

            var target = state.Players.First(p => p.GUID == takingPlayer.GUID);
            var cardsToMove = state.CurrentTrickPlay.Actions
                .Where(a => !a.IsPass && a.Trick != null)
                .SelectMany(a => a.Trick.Cards)
                .ToList();

            if (cardsToMove.Count > 0)
            {
                target.AddToDiscard(new List<Card>(cardsToMove));
            }

            state.CurrentTrickPlay.Clear();
            state.CurrentTrickPlay = new TrickPlay(state.Players.Where(p => !p.Finished).Count());
        }
    }
}
