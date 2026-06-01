using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Benchmark;
using Haggis.AI.Interfaces;
using Haggis.AI.Model;
using Haggis.AI.Strategies;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using MonteCarlo;

namespace Haggis.MctsRunAnalyzer
{
    public static class MctsRunAnalyzerRunner
    {
        public static MctsRunAnalyzerResult Run(MctsRunAnalyzerOptions options)
        {
            var playersBySeat = BuildPlayers(options).ToList();
            var game = new HaggisGame(playersBySeat);
            game.SetSeed(options.Seed);

            var state = game.NewRound();
            if (!(state.CurrentPlayer is AIPlayer aiPlayer))
            {
                throw new InvalidOperationException("Analyzer supports AI players only.");
            }

            if (!(aiPlayer.PlayStrategy is MonteCarloStrategy mctsStrategy))
            {
                throw new InvalidOperationException(
                    $"Current player '{aiPlayer.Name}' does not use MonteCarlo. Set ai1 to 'montecarlo' or an explicit montecarlo strategy.");
            }

            var traceEvents = new List<MctsTraceEvent>();
            MonteCarloResult computedResult = null;
            Action<MctsTraceEvent> traceHandler = traceEvent => traceEvents.Add(traceEvent);
            Action<MonteCarloResult> computedHandler = result => computedResult = result;

            var context = BuildTraceContext(options, state, 1, aiPlayer.Name);
            mctsStrategy.TraceContext = context;
            mctsStrategy.OnTrace += traceHandler;
            mctsStrategy.OnComputed += computedHandler;

            var action = aiPlayer.GetPlayingAction(state);
            var chosenAction = action?.Desc ?? string.Empty;

            return new MctsRunAnalyzerResult
            {
                Context = context,
                TargetPlayer = aiPlayer.Name,
                TargetStrategy = DescribeStrategy(options, options.Ai1Strategy, aiPlayer.PlayStrategy),
                ChosenAction = chosenAction,
                SetupLines = BuildSetupLines(state, options),
                TraceEvents = traceEvents,
                ComputedResult = computedResult
            };
        }

        private static IEnumerable<IHaggisPlayer> BuildPlayers(MctsRunAnalyzerOptions options)
        {
            yield return new AIPlayer("p1", CreateStrategy(options, options.Ai1Strategy));
            yield return new AIPlayer("p2", CreateStrategy(options, options.Ai2Strategy));
            yield return new AIPlayer("p3", CreateStrategy(options, options.Ai3Strategy));
        }

        private static IPlayStrategy CreateStrategy(MctsRunAnalyzerOptions options, string strategyName)
        {
            if (string.Equals(strategyName, "montecarlo", StringComparison.OrdinalIgnoreCase))
            {
                return new MonteCarloStrategy(options.Iterations, options.TimeBudgetMs, options.Workers);
            }

            return AiBenchmarkStrategyFactory.Create(strategyName);
        }

        private static string DescribeStrategy(MctsRunAnalyzerOptions options, string strategyName, IPlayStrategy strategy)
        {
            if (strategy is MonteCarloStrategy)
            {
                return string.Equals(strategyName, "montecarlo", StringComparison.OrdinalIgnoreCase)
                    ? $"montecarlo:{options.Iterations}:{options.TimeBudgetMs}:{options.Workers}"
                    : strategyName;
            }

            return string.IsNullOrWhiteSpace(strategyName)
                ? strategy?.GetType().Name ?? "(none)"
                : strategyName;
        }

        private static string BuildTraceContext(MctsRunAnalyzerOptions options, RoundState state, int decisionNumber, string playerName)
        {
            return
                $"seed={options.Seed} decision={decisionNumber} round={state.RoundNumber} moveIteration={state.MoveIteration + 1} player={playerName} ai1={options.Ai1Strategy} ai2={options.Ai2Strategy} ai3={options.Ai3Strategy}";
        }

        private static IReadOnlyList<string> BuildSetupLines(RoundState state, MctsRunAnalyzerOptions options)
        {
            var lines = new List<string>
            {
                $"round={state.RoundNumber} currentPlayer={state.CurrentPlayer?.Name} seed={options.Seed}",
                $"haggis={FormatCards(state.HaggisCards)}"
            };

            foreach (var player in state.Players.OrderBy(player => SeatNumber(player.Name)))
            {
                lines.Add($"hand {player.Name}: {FormatCards(player.Hand)}");
            }

            return lines;
        }

        private static string FormatCards(IEnumerable<Card> cards)
        {
            return cards == null
                ? string.Empty
                : string.Join(
                    " ",
                    cards
                        .OrderBy(card => card.Rank)
                        .ThenBy(card => card.Suit)
                        .Select(card => card.ToString()));
        }

        private static int SeatNumber(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName) || playerName.Length < 2)
            {
                return 0;
            }

            return int.TryParse(playerName.Substring(1), out var seat) ? seat : 0;
        }
    }

    public sealed class MctsRunAnalyzerResult
    {
        public string Context { get; set; }
        public string TargetPlayer { get; set; }
        public string TargetStrategy { get; set; }
        public string ChosenAction { get; set; }
        public IReadOnlyList<string> SetupLines { get; set; }
        public IReadOnlyList<MctsTraceEvent> TraceEvents { get; set; }
        public MonteCarloResult ComputedResult { get; set; }
    }
}
