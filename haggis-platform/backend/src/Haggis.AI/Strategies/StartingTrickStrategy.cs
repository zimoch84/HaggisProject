using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.Model;
using Haggis.AI.StartingTrickFilterStrategies;
using Haggis.AI.StartingTrickWeightStrategies;
using Haggis.Domain.Model;

namespace Haggis.AI.Strategies
{
    public sealed class StartingTrickStrategy : IPlayStrategy
    {
        public static System.Action<string> DiagnosticsSink { get; set; }

        private IStartingTrickFilterStrategy StartingTrickFilterStrategy { get; }
        private IReadOnlyList<IStartingTrickWeightStrategy> StartingTrickWeightStrategies { get; }

        public StartingTrickStrategy(
            IStartingTrickFilterStrategy startingTrickFilterStrategy = null,
            IStartingTrickWeightStrategy startingTrickWeightStrategy = null,
            IEnumerable<IStartingTrickWeightStrategy> startingTrickWeightStrategies = null,
            HeuristicOptions heuristicOptions = null)
        {
            heuristicOptions = heuristicOptions ?? new HeuristicOptions();
            StartingTrickFilterStrategy = startingTrickFilterStrategy ?? new FilterNoneStrategy();
            StartingTrickWeightStrategies = startingTrickWeightStrategies?.ToList()
                ?? (startingTrickWeightStrategy != null
                    ? new List<IStartingTrickWeightStrategy> { startingTrickWeightStrategy }
                    : new List<IStartingTrickWeightStrategy>
                    {
                        new PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(
                            heuristicOptions.PreferNonBreakableOpeningWeight),
                        new PreferLowerTricksWhenHandIsLargeWeightStrategy(
                            heuristicOptions.PreferLowerStartWeight),
                        new PreferShorterTricksWhenHandIsLargeWeightStrategy(
                            heuristicOptions.PreferShorterStartWeight),
                        new PenalizeBombOpeningWeightStrategy(
                            heuristicOptions.BombOpeningWeight),
                        new PenalizeWildCardsInOpeningWeightStrategy(
                            heuristicOptions.WildCardOpeningWeight),
                        new PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy(
                            heuristicOptions.HigherRelatedCombinationOpeningWeight),
                        new PreferTricksWithMoreContinuationsWeightStrategy(
                            heuristicOptions.ContinuationCountWeight),
                        new PreferSinglesNotBreakingNonWildCombinationsWeightStrategy(
                            heuristicOptions.PreferSinglesNotBreakingNonWildCombinationsWeight)
                    });
        }

        public HaggisAction GetPlayingAction(RoundState gameState)
        {
            if (gameState.CurrentTrickPlay.LastAction != null)
            {
                return null;
            }

            var possibleActions = gameState.PossibleActions.Where(a => !a.IsPass).ToList();
            if (!possibleActions.Any())
            {
                return null;
            }

            var aiPlayer = gameState.CurrentPlayer as AIPlayer;
            if (aiPlayer == null)
            {
                return null;
            }

            var suggestedTricks = aiPlayer.SuggestedTricks(null);
            var filteredTricks = ShouldBypassFiltering(possibleActions, gameState)
                ? new List<Trick>(suggestedTricks)
                : StartingTrickFilterStrategy.FilterTricks(new List<Trick>(suggestedTricks));
            var strategyWeights = StartingTrickWeightStrategies
                .Select(strategy => new
                {
                    StrategyName = strategy.GetType().Name,
                    Weights = strategy.GetWeight(suggestedTricks, gameState)
                })
                .ToList();

            var weightedActions = filteredTricks
                .Select(trick => new
                {
                    Trick = trick,
                    Action = HaggisAction.FromTrick(trick, gameState.CurrentPlayer)
                })
                .Where(x => possibleActions.Contains(x.Action))
                .Select(x => new
                {
                    x.Trick,
                    x.Action,
                    Breakdown = strategyWeights
                        .Select(strategy => (
                            StrategyName: strategy.StrategyName,
                            Weight: strategy.Weights
                                .Where(weight => ReferenceEquals(weight.Trick, x.Trick))
                                .Select(weight => weight.Weight)
                                .FirstOrDefault()))
                        .ToList()
                })
                .Select(x => new
                {
                    x.Trick,
                    x.Action,
                    x.Breakdown,
                    Weight = x.Breakdown.Sum(item => item.Weight)
                })
                .ToList();

            foreach (var candidate in weightedActions
                .OrderByDescending(candidate => candidate.Weight)
                .Take(20))
            {
                var breakdown = string.Join(
                    ", ",
                    candidate.Breakdown
                        .Select(item => $"{item.StrategyName}={item.Weight}"));
                WriteDiagnostic($"Starting trick candidate: {candidate.Trick}, weight: {candidate.Weight}, breakdown: {breakdown}");
            }

            return weightedActions
                .OrderByDescending(candidate => candidate.Weight)
                .Select(candidate => candidate.Action)
                .FirstOrDefault();
        }

        private static bool ShouldBypassFiltering(IList<HaggisAction> possibleActions, RoundState gameState)
        {
            var playableActions = possibleActions?.Where(action => !action.IsPass).ToList();
            if (playableActions == null || playableActions.Count != 1 || gameState == null)
            {
                return false;
            }

            var clonedState = gameState.Clone();
            var clonedAction = HaggisAction.FromTrick(playableActions[0].Trick, clonedState.CurrentPlayer, playableActions[0].IsFinal);
            clonedState.ApplyAction(clonedAction);
            return clonedState.RoundOver();
        }

        private static void WriteDiagnostic(string message)
        {
            Trace.WriteLine(message);
            DiagnosticsSink?.Invoke(message);
        }
    }
}
