using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarlo
{
    public sealed class MonteCarloMoveGenerationService : TrickGenerationServiceBase
    {
        private IMonteCarloTrickSelectionStrategy TrickSelectionStrategy { get; }
        private IMonteCarloActionSelectionStrategy ActionSelectionStrategy { get; }

        public MonteCarloMoveGenerationService(
            IMonteCarloActionSelectionStrategy actionSelectionStrategy = null,
            IMonteCarloTrickSelectionStrategy trickSelectionStrategy = null)
        {
            ActionSelectionStrategy = actionSelectionStrategy ?? new PreferFinalTrickMonteCarloActionsStrategy();
            TrickSelectionStrategy = trickSelectionStrategy ?? new SelectAllMonteCarloTricksStrategy();
        }

        public IList<HaggisAction> GetPossibleActionsForCurrentPlayer(HaggisGameState state)
        {
            if (state.RoundOver())
            {
                return new List<HaggisAction>();
            }

            var actions = new List<HaggisAction>();
            var lastTrick = state.CurrentTrickPlay.NotPassActions.LastOrDefault()?.Trick;
            var generatedTricks = lastTrick == null
                ? BuildPossibleOpeningTricks(state.CurrentPlayer)
                : BuildPossibleContinuationTricks(state.CurrentPlayer, lastTrick);

            var possibleTricks = TrickSelectionStrategy
                .Select(state, generatedTricks, lastTrick == null)
                .ToList();

            possibleTricks.ForEach(trick => actions.Add(HaggisAction.FromTrick(trick, state.CurrentPlayer)));

            var selectedActions = ActionSelectionStrategy.Select(state, actions);
            var hasFinalAction = selectedActions.Any(action => !action.IsPass && action.Trick != null && action.Trick.IsFinal);
            if (state.CurrentTrickPlay.LastAction != null && !hasFinalAction)
            {
                selectedActions.Add(HaggisAction.Pass(state.CurrentPlayer));
            }

            return selectedActions;
        }
    }
}
