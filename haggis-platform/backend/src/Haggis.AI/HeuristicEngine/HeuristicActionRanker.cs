using Haggis.AI.ContinuationTrickFilterStrategies;
using Haggis.AI.ContinuationTrickWeightStrategies;
using Haggis.AI.Interfaces;
using Haggis.AI.Model;
using Haggis.AI.StartingTrickFilterStrategies;
using Haggis.AI.StartingTrickWeightStrategies;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.AI.Strategies
{
    public sealed class HeuristicActionRanker
    {
        private const int PassCandidateThreshold = 0;

        private IStartingTrickFilterStrategy StartingTrickFilterStrategy { get; }
        private IReadOnlyList<IContinuationTrickFilterStrategy> ContinuationTrickFilterStrategies { get; }
        private IReadOnlyList<IStartingTrickWeightStrategy> StartingTrickWeightStrategies { get; }
        private IReadOnlyList<IContinuationTrickWeightStrategy> ContinuationTrickWeightStrategies { get; }

        public static HeuristicActionRanker CreateDefault(
            HeuristicOptions heuristicOptions = null,
            IStartingTrickFilterStrategy startingTrickFilterStrategy = null,
            IEnumerable<IContinuationTrickFilterStrategy> continuationTrickFilterStrategies = null)
        {
            heuristicOptions = heuristicOptions ?? new HeuristicOptions();

            return new HeuristicActionRanker(
                startingTrickFilterStrategy ?? new FilterNoneStrategy(),
                continuationTrickFilterStrategies?.ToList() ?? new List<IContinuationTrickFilterStrategy>
                {
                    new FilterDistinctContinuationStrategy(),
                    new FilterRedundantWildAssignmentsContinuationStrategy(),
                    new FilterNoneContinuationStrategy()
                },
                new List<IStartingTrickWeightStrategy>
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
                },
                new List<IContinuationTrickWeightStrategy>
                {
                    new PenalizeBombContinuationWeightStrategy(heuristicOptions.BombContinuationWeight),
                    new PenalizeWildCardsInContinuationWeightStrategy(heuristicOptions.WildCardContinuationWeight),
                    new PenalizeContinuationWhenHigherRelatedCombinationExistsWeightStrategy(
                        heuristicOptions.HigherRelatedCombinationContinuationWeight),
                    new PreferUsingWildAsHigherCardInContinuationWeightStrategy(
                        heuristicOptions.PreferUsingWildAsHigherCardInContinuationWeight),
                    new PreferLowerValueContinuationWeightStrategy(heuristicOptions.LowerValueContinuationWeight),
                    new PreferContinuationsWithFollowUpWeightStrategy(heuristicOptions.ContinuationFollowUpWeight),
                    new PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy(
                        heuristicOptions.PlayableBombInEndgameWeight)
                });
        }

        public HeuristicActionRanker(
            IStartingTrickFilterStrategy startingTrickFilterStrategy,
            IReadOnlyList<IContinuationTrickFilterStrategy> continuationTrickFilterStrategies,
            IReadOnlyList<IStartingTrickWeightStrategy> startingTrickWeightStrategies,
            IReadOnlyList<IContinuationTrickWeightStrategy> continuationTrickWeightStrategies)
        {
            StartingTrickFilterStrategy = startingTrickFilterStrategy;
            ContinuationTrickFilterStrategies = continuationTrickFilterStrategies;
            StartingTrickWeightStrategies = startingTrickWeightStrategies;
            ContinuationTrickWeightStrategies = continuationTrickWeightStrategies;
        }

        public IReadOnlyList<HeuristicRankedAction> RankOpeningActions(RoundState gameState, IList<HaggisAction> possibleActions)
        {
            if (gameState == null || possibleActions == null)
            {
                return new List<HeuristicRankedAction>();
            }

            var playableActions = possibleActions
                .Where(action => action != null && !action.IsPass)
                .ToList();
            if (!playableActions.Any())
            {
                return new List<HeuristicRankedAction>();
            }

            var suggestedTricks = GetSuggestedOpeningTricks(gameState, playableActions);
            var filteredTricks = ShouldBypassFiltering(playableActions, gameState)
                ? new List<Trick>(suggestedTricks)
                : StartingTrickFilterStrategy.FilterTricks(new List<Trick>(suggestedTricks));

            var weightedActions = BuildWeightedActions(playableActions, filteredTricks, suggestedTricks, gameState, StartingTrickWeightStrategies);
            return weightedActions
                .OrderByDescending(candidate => candidate.Weight)
                .ThenBy(candidate => candidate.OriginalIndex)
                .ToList();
        }

        public IReadOnlyList<HeuristicRankedAction> RankContinuationActions(RoundState gameState, IList<HaggisAction> possibleActions)
        {
            if (gameState == null || possibleActions == null)
            {
                return new List<HeuristicRankedAction>();
            }

            var passAction = possibleActions.FirstOrDefault(action => action != null && action.IsPass);
            var playableActions = possibleActions
                .Where(action => action != null && !action.IsPass)
                .ToList();
            if (!playableActions.Any())
            {
                return new List<HeuristicRankedAction>();
            }

            if (playableActions.Count == 1 && passAction == null)
            {
                return new List<HeuristicRankedAction>
                {
                    new HeuristicRankedAction
                    {
                        OriginalIndex = 0,
                        Action = playableActions[0],
                        Trick = playableActions[0].Trick,
                        Breakdown = new List<HeuristicWeightBreakdown>
                        {
                            new HeuristicWeightBreakdown
                            {
                                StrategyName = "auto-selected-single-legal-action",
                                Weight = 0
                            }
                        },
                        Weight = 0,
                        SortLabel = playableActions[0].Desc
                    }
                };
            }

            var suggestedTricks = playableActions
                .Select(action => action.Trick)
                .ToList();
            var filteredTricks = new List<Trick>(suggestedTricks);

            foreach (var filterStrategy in ContinuationTrickFilterStrategies)
            {
                filteredTricks = filterStrategy.FilterTricks(filteredTricks, gameState);
            }

            if (!filteredTricks.Any())
            {
                return new List<HeuristicRankedAction>();
            }

            var weightedActions = BuildWeightedActions(playableActions, filteredTricks, suggestedTricks, gameState, ContinuationTrickWeightStrategies)
                .ToList();
            var bestContinuationWeight = weightedActions.Any()
                ? weightedActions.Max(candidate => candidate.Weight)
                : int.MinValue;

            if (passAction != null && bestContinuationWeight <= PassCandidateThreshold)
            {
                weightedActions.Add(BuildPassCandidate(
                    passAction,
                    ContinuationTrickWeightStrategies.Select(strategy => strategy.GetType().Name)));
            }

            return weightedActions
                .OrderByDescending(candidate => candidate.Weight)
                .ThenBy(candidate => candidate.Action.IsPass)
                .ThenBy(candidate => candidate.Trick)
                .ThenBy(candidate => candidate.SortLabel)
                .ThenBy(candidate => candidate.OriginalIndex)
                .ToList();
        }

        private static List<Trick> GetSuggestedOpeningTricks(RoundState gameState, IList<HaggisAction> playableActions)
        {
            if (gameState.CurrentPlayer is AIPlayer aiPlayer)
            {
                return aiPlayer.SuggestedTricks(null);
            }

            return playableActions
                .Select(action => action.Trick)
                .Where(trick => trick != null)
                .ToList();
        }

        private static IEnumerable<HeuristicRankedAction> BuildWeightedActions<TWeightStrategy>(
            IList<HaggisAction> possibleActions,
            IList<Trick> filteredTricks,
            List<Trick> suggestedTricks,
            RoundState gameState,
            IReadOnlyList<TWeightStrategy> weightStrategies)
        {
            var strategyWeights = weightStrategies
                .Select(strategy => new
                {
                    StrategyName = strategy.GetType().Name,
                    Weights = GetWeights(strategy, suggestedTricks, gameState)
                })
                .ToList();

            return filteredTricks
                .Select((trick, filteredIndex) => new
                {
                    Trick = trick,
                    Action = HaggisAction.FromTrick(trick, gameState.CurrentPlayer),
                    OriginalIndex = filteredIndex
                })
                .Where(x => possibleActions.Contains(x.Action))
                .Select(x => new HeuristicRankedAction
                {
                    OriginalIndex = x.OriginalIndex,
                    Trick = x.Trick,
                    Action = x.Action,
                    Breakdown = strategyWeights
                        .Select(strategy => new HeuristicWeightBreakdown
                        {
                            StrategyName = strategy.StrategyName,
                            Weight = strategy.Weights
                                .Where(weight => ReferenceEquals(weight.Trick, x.Trick))
                                .Select(weight => weight.Weight)
                                .FirstOrDefault()
                        })
                        .ToList(),
                    SortLabel = x.Trick?.ToString() ?? x.Action.Desc
                })
                .Select(candidate =>
                {
                    candidate.Weight = candidate.Breakdown.Sum(item => item.Weight);
                    return candidate;
                });
        }

        private static IReadOnlyList<(int Weight, Trick Trick)> GetWeights<TWeightStrategy>(
            TWeightStrategy strategy,
            List<Trick> suggestedTricks,
            RoundState gameState)
        {
            if (strategy is IStartingTrickWeightStrategy startingStrategy)
            {
                return startingStrategy.GetWeight(suggestedTricks, gameState);
            }

            if (strategy is IContinuationTrickWeightStrategy continuationStrategy)
            {
                return continuationStrategy.GetWeight(suggestedTricks, gameState);
            }

            return new List<(int Weight, Trick Trick)>();
        }

        private static HeuristicRankedAction BuildPassCandidate(HaggisAction passAction, IEnumerable<string> strategyNames = null)
        {
            return new HeuristicRankedAction
            {
                OriginalIndex = int.MaxValue,
                Action = passAction,
                Trick = null,
                Breakdown = (strategyNames ?? Enumerable.Empty<string>())
                    .Select(strategyName => new HeuristicWeightBreakdown
                    {
                        StrategyName = strategyName,
                        Weight = 0
                    })
                    .ToList(),
                Weight = 0,
                SortLabel = passAction.Desc
            };
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
    }
}
