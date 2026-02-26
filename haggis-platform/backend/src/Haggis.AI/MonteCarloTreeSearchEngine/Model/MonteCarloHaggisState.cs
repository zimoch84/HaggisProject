using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarlo
{
    public sealed class MonteCarloHaggisState : IState<IHaggisPlayer, HaggisAction>
    {
        private HaggisGameState DomainState { get; }
        private IMonteCarloActionSelectionStrategy ActionSelectionStrategy { get; }
        private MonteCarloMoveGenerationService MoveGenerationService { get; }

        public MonteCarloHaggisState(
            HaggisGameState domainState,
            IMonteCarloActionSelectionStrategy actionSelectionStrategy = null)
        {
            DomainState = domainState;
            ActionSelectionStrategy = actionSelectionStrategy;
            MoveGenerationService = new MonteCarloMoveGenerationService(ActionSelectionStrategy);
        }

        public IHaggisPlayer CurrentPlayer => DomainState.CurrentPlayer;

        public IList<HaggisAction> Actions => MoveGenerationService.GetPossibleActionsForCurrentPlayer(DomainState);

        public void ApplyAction(HaggisAction action)
        {
            DomainState.ApplyAction(action);
        }

        public IState<IHaggisPlayer, HaggisAction> Clone()
        {
            return new MonteCarloHaggisState(DomainState.Clone(), ActionSelectionStrategy);
        }

        public double GetResult(IHaggisPlayer forPlayer)
        {
            var forPlayerScore = DomainState.Players.First(p => p.GUID == forPlayer.GUID).Score;
            var hasBetterPlayer = DomainState.Players.Any(p => p.Score > forPlayerScore);
            return hasBetterPlayer ? 0 : 1;
        }
    }
}
