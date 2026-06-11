using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Haggis.AI.ContinuationTrickFilterStrategies;
using Haggis.AI.ContinuationTrickWeightStrategies;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.Strategies
{
    public sealed class ContinuationTrickStrategy : IPlayStrategy
    {
        private const int PassCandidateThreshold = -100;

        public static System.Action<string> DiagnosticsSink { get; set; }

        private IReadOnlyList<IContinuationTrickFilterStrategy> ContinuationTrickFilterStrategies { get; }
        private IReadOnlyList<IContinuationTrickWeightStrategy> ContinuationTrickWeightStrategies { get; }

        public ContinuationTrickStrategy(
            IContinuationTrickFilterStrategy continuationTrickFilterStrategy = null,
            IContinuationTrickWeightStrategy continuationTrickWeightStrategy = null,
            IEnumerable<IContinuationTrickFilterStrategy> continuationTrickFilterStrategies = null,
            IEnumerable<IContinuationTrickWeightStrategy> continuationTrickWeightStrategies = null,
            HeuristicOptions heuristicOptions = null)
        {
            heuristicOptions = heuristicOptions ?? new HeuristicOptions();
            ContinuationTrickFilterStrategies = continuationTrickFilterStrategies?.ToList()
                ?? (continuationTrickFilterStrategy != null
                    ? new List<IContinuationTrickFilterStrategy> { continuationTrickFilterStrategy }
                    : new List<IContinuationTrickFilterStrategy>
                    {
                        new FilterDistinctContinuationStrategy(),
                        new FilterNoneContinuationStrategy()
                    });
            ContinuationTrickWeightStrategies = continuationTrickWeightStrategies?.ToList()
                ?? (continuationTrickWeightStrategy != null
                    ? new List<IContinuationTrickWeightStrategy> { continuationTrickWeightStrategy }
                    : new List<IContinuationTrickWeightStrategy>
                    {
                        new PenalizeBombContinuationWeightStrategy(heuristicOptions.BombContinuationPenaltyFactor),
                        new PenalizeWildCardsInContinuationWeightStrategy(heuristicOptions.WildCardContinuationPenaltyFactor),
                        new PenalizeContinuationWhenHigherRelatedCombinationExistsWeightStrategy(
                            heuristicOptions.HigherRelatedCombinationContinuationPenaltyFactor),
                        new PreferLowerValueContinuationWeightStrategy(heuristicOptions.LowerValueContinuationWeight),
                        new PreferContinuationsWithFollowUpWeightStrategy(heuristicOptions.ContinuationFollowUpWeight),
                        new PreferShorterContinuationWeightStrategy(heuristicOptions.ShorterContinuationWeight)
                    });
        }

        public HaggisAction GetPlayingAction(RoundState gameState)
        {
            if (gameState?.CurrentTrickPlay?.LastNotPassAction == null)
            {
                return null;
            }

            var passAction = gameState.PossibleActions
                .FirstOrDefault(action => action.IsPass);
            var possibleActions = gameState.PossibleActions
                .Where(action => !action.IsPass)
                .ToList();
            if (!possibleActions.Any())
            {
                WriteDiagnostic("Continuation trick candidate: none, reason: no playable continuation actions");
                return null;
            }

            if (possibleActions.Count == 1 && passAction == null)
            {
                var onlyAction = possibleActions[0];
                WriteDiagnostic($"Continuation trick candidate: {onlyAction.Trick}, weight: 0, breakdown: auto-selected-single-legal-action=0");
                return possibleActions[0];
            }

            var suggestedTricks = possibleActions
                .Select(action => action.Trick)
                .ToList();
            var filteredTricks = new List<Trick>(suggestedTricks);

            foreach (var filterStrategy in ContinuationTrickFilterStrategies)
            {
                filteredTricks = filterStrategy.FilterTricks(filteredTricks, gameState);
            }

            if (!filteredTricks.Any())
            {
                WriteDiagnostic("Continuation trick candidate: none, reason: all continuation candidates filtered out");
                return null;
            }

            var strategyWeights = ContinuationTrickWeightStrategies
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
                    Weight = x.Breakdown.Sum(item => item.Weight),
                    SortLabel = x.Trick.ToString()
                })
                .ToList();

            var bestContinuationWeight = weightedActions.Any()
                ? weightedActions.Max(candidate => candidate.Weight)
                : int.MinValue;

            if (passAction != null && bestContinuationWeight <= PassCandidateThreshold)
            {
                weightedActions.Add(new
                {
                    Trick = (Trick)null,
                    Action = passAction,
                    Breakdown = strategyWeights
                        .Select(strategy => (strategy.StrategyName, Weight: 0))
                        .ToList(),
                    Weight = 0,
                    SortLabel = passAction.Desc
                });
            }

            foreach (var candidate in weightedActions
                .OrderByDescending(candidate => candidate.Weight)
                .ThenBy(candidate => candidate.Action.IsPass)
                .ThenBy(candidate => candidate.Trick)
                .ThenBy(candidate => candidate.SortLabel)
                .Take(20))
            {
                var breakdown = string.Join(
                    ", ",
                    candidate.Breakdown.Select(item => $"{item.StrategyName}={item.Weight}"));
                WriteDiagnostic($"Continuation trick candidate: {(candidate.Action.IsPass ? candidate.Action.Desc : candidate.Trick.ToString())}, weight: {candidate.Weight}, breakdown: {breakdown}");
            }

            return weightedActions
                .OrderByDescending(candidate => candidate.Weight)
                .ThenBy(candidate => candidate.Action.IsPass)
                .ThenBy(candidate => candidate.Trick)
                .ThenBy(candidate => candidate.SortLabel)
                .Select(candidate => candidate.Action)
                .FirstOrDefault();
        }

        private static void WriteDiagnostic(string message)
        {
            Trace.WriteLine(message);
            DiagnosticsSink?.Invoke(message);
        }
    }
}
