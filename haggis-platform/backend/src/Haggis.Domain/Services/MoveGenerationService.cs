using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.Domain.Services
{
    public sealed class MoveGenerationService : TrickGenerationServiceBase, ITrickGenerationService
    {
        public List<Trick> GetPossibleOpeningTricks(IHaggisPlayer player)
        {
            return BuildPossibleOpeningTricks(player);
        }

        public List<Trick> GetPossibleContinuationTricks(IHaggisPlayer player, Trick lastTrick)
        {
            return BuildPossibleContinuationTricks(player, lastTrick);
        }

        public IList<HaggisAction> GetPossibleActionsForCurrentPlayer(HaggisGameState state)
        {
            if (state.RoundOver())
            {
                return new List<HaggisAction>();
            }

            var actions = new List<HaggisAction>();
            var lastTrick = state.CurrentTrickPlay.NotPassActions.LastOrDefault()?.Trick;
            var possibleTricks = lastTrick == null
                ? GetPossibleOpeningTricks(state.CurrentPlayer)
                : GetPossibleContinuationTricks(state.CurrentPlayer, lastTrick);

            possibleTricks.ForEach(trick => actions.Add(HaggisAction.FromTrick(trick, state.CurrentPlayer)));
            actions.Add(HaggisAction.Pass(state.CurrentPlayer));

            return actions;
        }
    }
}
