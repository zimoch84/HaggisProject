using Haggis.AI.Interfaces;
using Haggis.AI.Model;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using MonteCarlo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Haggis.AI.Strategies
{
    public class MonteCarloStrategy : IPlayStrategy
    {
        private int Simulations { get; }
        private long TimeBudget { get; }
        private int Workers { get; }
        private IMonteCarloActionSelectionStrategy ActionSelectionStrategy { get; }
        private MonteCarloHeuristicOptions HeuristicOptions { get; }

        public event Action<MonteCarloResult> OnComputed;
        public event Action<MctsTraceEvent> OnTrace;
        public string TraceContext { get; set; }
        public bool CaptureTiming { get; set; }

        private static JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public MonteCarloStrategy(
            int simulations,
            long timeBudget,
            int? workers = null,
            IMonteCarloActionSelectionStrategy actionSelectionStrategy = null,
            MonteCarloHeuristicOptions heuristicOptions = null)
        {
            Simulations = simulations;
            TimeBudget = timeBudget;
            Workers = workers ?? Math.Max(1, Environment.ProcessorCount - 1);
            ActionSelectionStrategy = actionSelectionStrategy;
            HeuristicOptions = heuristicOptions;
        }

        public HaggisAction GetPlayingAction(RoundState gameState)
        {
            var legalActionsCount = gameState.PossibleActions.Count;
            var timer = Stopwatch.StartNew();
            var searchResult = Search(
                gameState,
                Simulations,
                TimeBudget,
                Workers,
                unchecked((int)gameState.MoveIteration),
                TraceContext,
                OnTrace,
                ActionSelectionStrategy,
                HeuristicOptions,
                CaptureTiming);
            timer.Stop();
            var actions = searchResult.TopActions.Select(a => new MonteCarloActionInfo
            {
                Action = a.Action,
                NumRuns = a.NumRuns,
                NumWins = a.NumWins
            }).ToList();

            var result = new MonteCarloResult
            {
                Player = gameState.CurrentPlayer,
                Actions = actions,
                Iterations = actions.Sum(action => action.NumRuns),
                BudgetMs = TimeBudget,
                ElapsedMs = timer.ElapsedMilliseconds,
                LegalActionsCount = legalActionsCount,
                RootChildrenCount = actions.Count,
                Workers = searchResult.Workers,
                ScheduledRollouts = searchResult.ScheduledRollouts,
                CompletedRollouts = searchResult.CompletedRollouts,
                TreeNodeCount = searchResult.TreeNodeCount,
                TreeMaxDepth = searchResult.TreeMaxDepth,
                Timing = searchResult.Timing
            };

            OnComputed?.Invoke(result);

            return result.Actions.First().Action;
        }

        public static IEnumerable<IMctsNode<MonteCarloHaggisAction>> GetTopActions(RoundState gameState, int maxIteration, long timeBudget)
        {
            return GetTopActions(gameState, maxIteration, timeBudget, null);
        }

        public static IEnumerable<IMctsNode<MonteCarloHaggisAction>> GetTopActions(
            RoundState gameState,
            int maxIteration,
            long timeBudget,
            IMonteCarloActionSelectionStrategy actionSelectionStrategy)
        {
            return Search(gameState, maxIteration, timeBudget, 1, unchecked((int)gameState.MoveIteration), null, null, actionSelectionStrategy, null, false)
                .TopActions
                .ToList();
        }

        public static IEnumerable<IMctsNode<MonteCarloHaggisAction>> GetTopActions(RoundState gameState, int maxIteration)
        {
            var gameStateClone = gameState.Clone();
            var monteCarloState = new MonteCarloHaggisState(gameStateClone);
            return MonteCarloTreeSearch.GetTopActions(monteCarloState, maxIteration).ToList();
        }

        private static MctsSearchResult<MonteCarloHaggisAction> Search(
            RoundState gameState,
            int maxIteration,
            long timeBudget,
            int workers,
            int seed,
            string traceContext,
            Action<MctsTraceEvent> trace,
            IMonteCarloActionSelectionStrategy actionSelectionStrategy,
            MonteCarloHeuristicOptions heuristicOptions,
            bool captureTiming)
        {
            var gameStateClone = gameState.Clone();
            var timing = captureTiming ? new MctsTimingCollector() : null;
            var rolloutSelectionStrategy = BuildRolloutSelectionStrategy(heuristicOptions);
            var treeSelectionStrategy = BuildTreeSelectionStrategy(actionSelectionStrategy, heuristicOptions);
            var monteCarloState = new MonteCarloHaggisState(gameStateClone, treeSelectionStrategy, rolloutSelectionStrategy, timing);
            return MonteCarloTreeSearch.Search<MonteCarloHaggisPlayer, MonteCarloHaggisAction>(
                monteCarloState,
                new MctsOptions
                {
                    MaxIterations = maxIteration,
                    TimeBudgetMs = timeBudget,
                Workers = workers,
                Seed = seed,
                TraceContext = traceContext,
                Trace = trace,
                Timing = timing
                });
        }

        private static IMonteCarloActionSelectionStrategy BuildTreeSelectionStrategy(
            IMonteCarloActionSelectionStrategy actionSelectionStrategy,
            MonteCarloHeuristicOptions heuristicOptions)
        {
            if (actionSelectionStrategy != null)
            {
                return actionSelectionStrategy;
            }

            if (heuristicOptions?.Enabled == true && heuristicOptions.TreeTopN > 0)
            {
                return new HeuristicMonteCarloActionSelectionStrategy(
                    heuristicOptions.HeuristicOptions,
                    heuristicOptions.TreeTopN);
            }

            return null;
        }

        private static IMonteCarloRolloutSelectionStrategy BuildRolloutSelectionStrategy(MonteCarloHeuristicOptions heuristicOptions)
        {
            if (heuristicOptions?.Enabled == true && heuristicOptions.RolloutTopN > 0)
            {
                return new HeuristicMonteCarloRolloutSelectionStrategy(
                    heuristicOptions.HeuristicOptions,
                    heuristicOptions.RolloutTopN);
            }

            return null;
        }
    }
}
