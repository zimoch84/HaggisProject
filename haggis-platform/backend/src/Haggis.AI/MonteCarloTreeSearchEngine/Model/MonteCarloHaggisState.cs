using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarlo
{
    public sealed class MonteCarloHaggisState : IState<MonteCarloHaggisPlayer, MonteCarloHaggisAction>
    {
        public RoundState DomainState { get; }
        private IMonteCarloActionSelectionStrategy ActionSelectionStrategy { get; }
        private MonteCarloMoveGenerationService MoveGenerationService { get; }
        private MctsTimingCollector Timing { get; }

        public MonteCarloHaggisState(
            RoundState domainState,
            IMonteCarloActionSelectionStrategy actionSelectionStrategy = null,
            MctsTimingCollector timing = null)
        {
            DomainState = domainState;
            ActionSelectionStrategy = actionSelectionStrategy;
            Timing = timing;
            MoveGenerationService = new MonteCarloMoveGenerationService(ActionSelectionStrategy, null, timing);
        }

        public MonteCarloHaggisPlayer CurrentPlayer => new MonteCarloHaggisPlayer(DomainState.CurrentPlayer);

        public IList<MonteCarloHaggisAction> Actions => MoveGenerationService.GetPossibleActionsForCurrentPlayer(DomainState);

        public void ApplyAction(MonteCarloHaggisAction action)
        {
            DomainState.ApplyAction(action);
        }

        public IState<MonteCarloHaggisPlayer, MonteCarloHaggisAction> Clone()
        {
            return new MonteCarloHaggisState(DomainState.Clone(), ActionSelectionStrategy, Timing);
        }

        public double GetResult(MonteCarloHaggisPlayer forPlayer)
        {
            if (!DomainState.RoundOver())
            {
                return 0;
            }

            var roundPointsByPlayer = BuildRoundPointsByPlayer();
            var forPlayerScore = roundPointsByPlayer[forPlayer.DomainPlayer.GUID];
            var hasBetterPlayer = roundPointsByPlayer.Values.Any(score => score > forPlayerScore);
            return hasBetterPlayer ? 0 : 1;
        }

        private Dictionary<Guid, int> BuildRoundPointsByPlayer()
        {
            var points = DomainState.Players.ToDictionary(
                player => player.GUID,
                _ => 0);

            var haggisPoints = DomainState.HaggisCards?.Sum(card => DomainState.ScoringStrategy.GetCardPoints(card)) ?? 0;
            var firstFinishedGuid = DomainState.FinishingOrder.FirstOrDefault();

            foreach (var player in DomainState.Players)
            {
                var tricksPoints = player.Discard.Sum(card => DomainState.ScoringStrategy.GetCardPoints(card));
                var runOutPoints = player.OpponentRemainingCardsOnFinish < 0
                    ? 0
                    : player.OpponentRemainingCardsOnFinish * DomainState.ScoringStrategy.RunOutMultiplier;
                var bonusHaggisPoints = firstFinishedGuid == player.GUID ? haggisPoints : 0;

                points[player.GUID] = tricksPoints + runOutPoints + bonusHaggisPoints;
            }

            return points;
        }
    }
}
