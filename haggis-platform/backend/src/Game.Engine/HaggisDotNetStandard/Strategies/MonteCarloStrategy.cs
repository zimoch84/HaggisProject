using Haggis.Interfaces;
using Haggis.Model;
using MonteCarlo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Haggis.Strategies
{
    public class MonteCarloStrategy : IPlayStrategy
    {
        private int _simulations;
        private long _timeBudget;

        public event Action<MonteCarloResult> OnComputed;

        private static JsonSerializerSettings jsonsettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };
        public MonteCarloStrategy(int simulations, long timeBudget)
        {
            _simulations = simulations;
            _timeBudget = timeBudget;

        }

        public HaggisAction GetPlayingAction(HaggisGameState gameState)
        {
            var top = GetTopActions(gameState, _simulations, _timeBudget).ToList();

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

            // 🟩 WYWOŁANIE EVENTU TUTAJ
            OnComputed?.Invoke(result);

            return result.Actions.First().Action;
        }

        public static IEnumerable<IMctsNode<HaggisAction>> GetTopActions(HaggisGameState gameState, int maxIteration, long timeBudget)
        {
            var gameStateClone = gameState.Clone();
            return MonteCarloTreeSearch.GetTopActions(gameStateClone, maxIteration, timeBudget).ToList();
        }

        public static IEnumerable<IMctsNode<HaggisAction>> GetTopActions(HaggisGameState gameState, int maxIteration)
        {
            var gameStateClone = gameState.Clone();
            return MonteCarloTreeSearch.GetTopActions(gameStateClone, maxIteration).ToList();
        }

        private static void LogActionsToFile(IHaggisPlayer player, IEnumerable<IMctsNode<HaggisAction>> actions)
        {
            var json = JsonConvert.SerializeObject(actions, Formatting.Indented, jsonsettings);
            string filePath = String.Format("../../../files/slawek_{0:yyyyMMddHHmmssfff}.json", DateTime.Now);
            File.WriteAllText(filePath, json);
        }
    }
}
