using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarlo
{
    public sealed class MonteCarloHaggisState : IState<MonteCarloHaggisPlayer, MonteCarloHaggisAction>
    {
        private RoundState DomainState { get; }
        private IMonteCarloActionSelectionStrategy ActionSelectionStrategy { get; }
        private MonteCarloMoveGenerationService MoveGenerationService { get; }

        public MonteCarloHaggisState(
            RoundState domainState,
            IMonteCarloActionSelectionStrategy actionSelectionStrategy = null)
        {
            DomainState = domainState;
            ActionSelectionStrategy = actionSelectionStrategy;
            MoveGenerationService = new MonteCarloMoveGenerationService(ActionSelectionStrategy);
        }

        public MonteCarloHaggisPlayer CurrentPlayer => new MonteCarloHaggisPlayer(DomainState.CurrentPlayer);

        public IList<MonteCarloHaggisAction> Actions => MoveGenerationService.GetPossibleActionsForCurrentPlayer(DomainState);

        public void ApplyAction(MonteCarloHaggisAction action)
        {
            DomainState.ApplyAction(action);
        }

        public IState<MonteCarloHaggisPlayer, MonteCarloHaggisAction> Clone()
        {
            return new MonteCarloHaggisState(DomainState.Clone(), ActionSelectionStrategy);
        }

        public double GetResult(MonteCarloHaggisPlayer forPlayer)
        {
            var forPlayerScore = DomainState.Players.First(p => p.GUID == forPlayer.DomainPlayer.GUID).Score;
            var hasBetterPlayer = DomainState.Players.Any(p => p.Score > forPlayerScore);
            return hasBetterPlayer ? 0 : 1;
        }
    }
}
