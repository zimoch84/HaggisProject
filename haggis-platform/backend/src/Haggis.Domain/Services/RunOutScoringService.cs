using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Linq;

namespace Haggis.Domain.Services
{
    public sealed class RunOutScoringService : IRunOutScoringService
    {
        public void Apply(HaggisGameState state, HaggisAction action)
        {
            if (action == null || action.Player == null)
                return;

            var target = state.Players.FirstOrDefault(p => p.GUID == action.Player.GUID);
            if (target == null || !target.Finished)
                return;

            foreach (var player in state.Players.Where(p => !p.Finished && p.GUID != action.Player.GUID))
            {
                target.Score += player.Hand.Count * state.ScoringStrategy.RunOutMultiplier;
            }
        }
    }
}
