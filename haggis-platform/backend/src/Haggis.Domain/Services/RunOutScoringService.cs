using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Linq;

namespace Haggis.Domain.Services
{
    public sealed class RunOutScoringService : IRoundScoringService
    {
        public void Apply(RoundState state, HaggisAction action)
        {
            if (action == null || action.Player == null)
                return;

            var target = state.Players.FirstOrDefault(p => p.GUID == action.Player.GUID);
            if (target == null || !target.Finished)
                return;

            state.RegisterPlayerFinished(target);

            if (target.OpponentRemainingCardsOnFinish >= 0)
            {
                return;
            }

            target.OpponentRemainingCardsOnFinish = state.Players
                .Where(p => !p.Finished && p.GUID != action.Player.GUID)
                .Sum(player => player.Hand.Count);
        }
    }
}
