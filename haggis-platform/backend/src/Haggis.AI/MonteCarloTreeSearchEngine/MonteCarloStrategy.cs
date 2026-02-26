using Haggis.AI.Interfaces;
using Haggis.AI.Model;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using MonteCarlo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Haggis.AI.Strategies
{
    public class MonteCarloStrategy : IPlayStrategy
    {
        private int Simulations { get; }
        private long TimeBudget { get; }
        private IMonteCarloActionSelectionStrategy ActionSelectionStrategy { get; }

        public event Action<MonteCarloResult> OnComputed;

        private static JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public MonteCarloStrategy(
            int simulations,
            long timeBudget,
            IMonteCarloActionSelectionStrategy actionSelectionStrategy = null)
        {
            Simulations = simulations;
            TimeBudget = timeBudget;
            ActionSelectionStrategy = actionSelectionStrategy;
        }

        public HaggisAction GetPlayingAction(HaggisGameState gameState)
        {
            var top = GetTopActions(gameState, Simulations, TimeBudget, ActionSelectionStrategy).ToList();

            var result = new MonteCarloResult
            {
                Player = gameState.CurrentPlayer,
                Actions = top.Select(a => new MonteCarloActionInfo
                {
                    Action = a.Action,
                    NumRuns = a.NumRuns,
                    NumWins = a.NumWins
                }).ToList()
            };

            OnComputed?.Invoke(result);

            return result.Actions.First().Action;
        }

        public static IEnumerable<IMctsNode<HaggisAction>> GetTopActions(HaggisGameState gameState, int maxIteration, long timeBudget)
        {
            return GetTopActions(gameState, maxIteration, timeBudget, null);
        }

        public static IEnumerable<IMctsNode<HaggisAction>> GetTopActions(
            HaggisGameState gameState,
            int maxIteration,
            long timeBudget,
            IMonteCarloActionSelectionStrategy actionSelectionStrategy)
        {
            var gameStateClone = gameState.Clone();
            var monteCarloState = new MonteCarloHaggisState(gameStateClone, actionSelectionStrategy);
            return MonteCarloTreeSearch.GetTopActions(monteCarloState, maxIteration, timeBudget).ToList();
        }

        public static IEnumerable<IMctsNode<HaggisAction>> GetTopActions(HaggisGameState gameState, int maxIteration)
        {
            var gameStateClone = gameState.Clone();
            var monteCarloState = new MonteCarloHaggisState(gameStateClone);
            return MonteCarloTreeSearch.GetTopActions(monteCarloState, maxIteration).ToList();
        }
    }
}
