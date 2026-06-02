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

        public IList<MonteCarloHaggisAction> GetPossibleActionsForCurrentPlayer(RoundState state)
        {
            if (state.RoundOver())
            {
                return new List<MonteCarloHaggisAction>();
            }

            var lastTrick = state.CurrentTrickPlay.LastNotPassTrick;
            var generatedTricks = lastTrick == null
                ? BuildPossibleOpeningTricks(state.CurrentPlayer)
                : BuildPossibleContinuationTricks(state.CurrentPlayer, lastTrick);

            var generatedActions = TrickSelectionStrategy
                .Select(state, generatedTricks, lastTrick == null)
                .Select(trick => MonteCarloHaggisAction.FromTrick(trick, state.CurrentPlayer))
                .ToList();

            var selectedActions = ActionSelectionStrategy.Select(state, generatedActions).ToList();
            var hasFinalAction = selectedActions.Any(action => !action.IsPass && action.Trick != null && action.Trick.IsFinal);
            if (state.CurrentTrickPlay.LastAction != null && !hasFinalAction)
            {
                selectedActions.Add(MonteCarloHaggisAction.Pass(state.CurrentPlayer));
            }

            return selectedActions;
        }
    }
}
