using System;
using System.Collections.Generic;
using System.IO;
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
            if (!string.IsNullOrWhiteSpace(options.ReplayLogPath))
            {
                return RunFromReplayLog(options);
            }

            var playersBySeat = BuildPlayers(options).ToList();
            var game = new HaggisGame(playersBySeat);
            game.SetSeed(options.Seed);
            var decisionNumber = 0;
            var state = game.NewRound();

            while (true)
            {
                if (!(state.CurrentPlayer is AIPlayer aiPlayer))
                {
                    throw new InvalidOperationException("Analyzer supports AI players only.");
                }

                decisionNumber++;
                var currentMove = state.MoveIteration + 1;
                if (state.RoundNumber == options.RoundNumber && currentMove == options.MoveNumber)
                {
                    return AnalyzeTargetDecision(options, state, aiPlayer, decisionNumber);
                }

                var action = aiPlayer.GetPlayingAction(state);
                if (action == null)
                {
                    throw new InvalidOperationException(
                        $"AI player '{aiPlayer.Name}' produced no action at round={state.RoundNumber}, move={currentMove}.");
                }

                state.ApplyAction(action);
                if (!state.RoundOver())
                {
                    continue;
                }

                game.RegisterRoundScoringResult(state);
                if (game.GameOver())
                {
                    throw new InvalidOperationException(
                        $"Target decision round={options.RoundNumber}, move={options.MoveNumber} was not reached before game over.");
                }

                state = game.NewRound();
            }
        }

        private static MctsRunAnalyzerResult RunFromReplayLog(MctsRunAnalyzerOptions options)
        {
            var replay = ParseReplayLog(options.ReplayLogPath);
            var players = BuildPlayersFromReplay(options, replay).ToList();
            var game = new HaggisGame(players);
            game.SetSeed(replay.Seed ?? options.Seed);

            var state = game.NewRound();
            foreach (var replayMove in replay.Moves)
            {
                while (state.RoundNumber < replayMove.RoundNumber)
                {
                    if (!state.RoundOver())
                    {
                        throw new InvalidOperationException(
                            $"Replay desync before round transition. State round={state.RoundNumber} move={state.MoveIteration + 1}, replay round={replayMove.RoundNumber} move={replayMove.MoveNumber}.");
                    }

                    game.RegisterRoundScoringResult(state);
                    if (game.GameOver())
                    {
                        throw new InvalidOperationException("Replay reached game over before target decision.");
                    }

                    state = game.NewRound();
                }

                var currentMove = state.MoveIteration + 1;
                if (state.RoundNumber == options.RoundNumber && currentMove == options.MoveNumber)
                {
                    return AnalyzeTargetDecision(options, state, replayMove.PlayerName, replayMove.StrategyName, replay);
                }

                if (state.RoundNumber != replayMove.RoundNumber || currentMove != replayMove.MoveNumber)
                {
                    throw new InvalidOperationException(
                        $"Replay move mismatch. State round={state.RoundNumber} move={currentMove}, replay round={replayMove.RoundNumber} move={replayMove.MoveNumber}.");
                }

                if (!string.Equals(state.CurrentPlayer.Name, replayMove.PlayerName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Replay player mismatch at round={replayMove.RoundNumber} move={replayMove.MoveNumber}. State player={state.CurrentPlayer.Name}, replay player={replayMove.PlayerName}.");
                }

                var action = ResolveReplayAction(state, replayMove.SelectedAction);
                state.ApplyAction(action);
            }

            throw new InvalidOperationException(
                $"Target decision round={options.RoundNumber}, move={options.MoveNumber} was not reached in replay log.");
        }

        private static MctsRunAnalyzerResult AnalyzeTargetDecision(
            MctsRunAnalyzerOptions options,
            RoundState state,
            AIPlayer aiPlayer,
            int decisionNumber)
        {
            if (!(aiPlayer.PlayStrategy is MonteCarloStrategy mctsStrategy))
            {
                throw new InvalidOperationException(
                    $"Current player '{aiPlayer.Name}' at round={state.RoundNumber}, move={state.MoveIteration + 1} does not use MonteCarlo.");
            }

            var traceEvents = new List<MctsTraceEvent>();
            MonteCarloResult computedResult = null;
            Action<MctsTraceEvent> traceHandler = traceEvent => traceEvents.Add(traceEvent);
            Action<MonteCarloResult> computedHandler = result => computedResult = result;

            var context = BuildTraceContext(options, state, decisionNumber, aiPlayer.Name);
            mctsStrategy.TraceContext = context;
            mctsStrategy.OnTrace += traceHandler;
            mctsStrategy.OnComputed += computedHandler;

            try
            {
                var action = aiPlayer.GetPlayingAction(state);
                var chosenAction = action?.Desc ?? string.Empty;

                return new MctsRunAnalyzerResult
                {
                    Context = context,
                    TargetPlayer = aiPlayer.Name,
                    TargetStrategy = DescribeStrategy(options, StrategyForSeat(options, aiPlayer.Name), aiPlayer.PlayStrategy),
                    ChosenAction = chosenAction,
                    SetupLines = BuildSetupLines(state, options),
                    HeuristicRanking = BuildHeuristicRanking(options, state, aiPlayer.Name),
                    TraceEvents = traceEvents,
                    ComputedResult = computedResult
                };
            }
            finally
            {
                mctsStrategy.OnTrace -= traceHandler;
                mctsStrategy.OnComputed -= computedHandler;
            }
        }

        private static MctsRunAnalyzerResult AnalyzeTargetDecision(
            MctsRunAnalyzerOptions options,
            RoundState state,
            string expectedPlayerName,
            string replayStrategyName,
            ReplayLog replay)
        {
            if (!(state.CurrentPlayer is AIPlayer aiPlayer))
            {
                throw new InvalidOperationException(
                    $"Target decision round={state.RoundNumber}, move={state.MoveIteration + 1} belongs to non-AI player '{state.CurrentPlayer?.Name}'.");
            }

            if (!string.Equals(aiPlayer.Name, expectedPlayerName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Target decision player mismatch. State player={aiPlayer.Name}, replay player={expectedPlayerName}.");
            }

            var result = AnalyzeTargetDecision(options, state, aiPlayer, replay.DecisionNumber(state.RoundNumber, state.MoveIteration + 1));
            if (!string.IsNullOrWhiteSpace(replayStrategyName))
            {
                result.TargetStrategy = replayStrategyName;
            }

            if (replay.Seed.HasValue)
            {
                var setup = result.SetupLines?.ToList() ?? new List<string>();
                setup.Add($"replay-log={Path.GetFullPath(options.ReplayLogPath)}");
                result.SetupLines = setup;
            }

            return result;
        }

        private static IEnumerable<IHaggisPlayer> BuildPlayers(MctsRunAnalyzerOptions options)
        {
            yield return new AIPlayer("p1", CreateStrategy(options, options.Ai1Strategy));
            yield return new AIPlayer("p2", CreateStrategy(options, options.Ai2Strategy));
            if (options.Players == 3)
            {
                yield return new AIPlayer("p3", CreateStrategy(options, options.Ai3Strategy));
            }
        }

        private static IEnumerable<IHaggisPlayer> BuildPlayersFromReplay(MctsRunAnalyzerOptions options, ReplayLog replay)
        {
            foreach (var playerName in replay.PlayerNames)
            {
                if (string.Equals(playerName, "AI-1", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new AIPlayer(playerName, CreateStrategy(options, options.Ai1Strategy));
                    continue;
                }

                if (string.Equals(playerName, "AI-2", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new AIPlayer(playerName, CreateStrategy(options, options.Ai2Strategy));
                    continue;
                }

                if (string.Equals(playerName, "AI-3", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new AIPlayer(playerName, CreateStrategy(options, options.Ai3Strategy));
                    continue;
                }

                yield return new HaggisPlayer(playerName);
            }
        }

        private static IPlayStrategy CreateStrategy(MctsRunAnalyzerOptions options, string strategyName)
        {
            if (string.Equals(strategyName, "montecarlo", StringComparison.OrdinalIgnoreCase))
            {
                return new MonteCarloStrategy(options.Iterations, options.TimeBudgetMs, options.Workers);
            }

            if (TryParseMonteCarloStrategy(strategyName, out var monteCarloHeuristicOptions))
            {
                return new MonteCarloStrategy(
                    options.Iterations,
                    options.TimeBudgetMs,
                    options.Workers,
                    null,
                    monteCarloHeuristicOptions);
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
                $"round={state.RoundNumber} move={state.MoveIteration + 1} currentPlayer={state.CurrentPlayer?.Name} seed={options.Seed}",
                $"haggis={FormatCards(state.HaggisCards)}",
                $"current-trick-last-action={state.CurrentTrickPlay?.LastAction?.Desc ?? "(none)"}",
                $"current-trick-last-not-pass={state.CurrentTrickPlay?.LastNotPassAction?.Desc ?? "(none)"}"
            };

            foreach (var player in state.Players.OrderBy(player => SeatNumber(player.Name)))
            {
                lines.Add($"hand {player.Name}: {FormatCards(player.Hand)}");
                lines.Add($"discard {player.Name}: {FormatCards(player.Discard)}");
            }

            return lines;
        }

        private static string StrategyForSeat(MctsRunAnalyzerOptions options, string playerName)
        {
            return SeatNumber(playerName) switch
            {
                1 => options.Ai1Strategy,
                2 => options.Ai2Strategy,
                3 => options.Ai3Strategy,
                _ => null
            };
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
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return 0;
            }

            if ((playerName.StartsWith("AI-", StringComparison.OrdinalIgnoreCase) ||
                 playerName.StartsWith("ai-", StringComparison.OrdinalIgnoreCase)) &&
                int.TryParse(playerName.Substring(3), out var aiSeat))
            {
                return aiSeat;
            }

            if (playerName.Length >= 2 && int.TryParse(playerName.Substring(1), out var seat))
            {
                return seat;
            }

            return 0;
        }

        private static bool TryParseMonteCarloStrategy(
            string strategyName,
            out MonteCarloHeuristicOptions heuristicOptions)
        {
            heuristicOptions = null;

            var parts = (strategyName ?? string.Empty).Trim().Split(':');
            if (parts.Length < 3 || parts.Length > 6 ||
                !string.Equals(parts[0], "montecarlo", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (parts.Length <= 4)
            {
                return true;
            }

            var heuristicStartIndex = 4;
            if (parts.Length == 4)
            {
                heuristicStartIndex = 3;
            }

            if (parts.Length - heuristicStartIndex != 2)
            {
                return false;
            }

            if (!int.TryParse(parts[heuristicStartIndex], out var treeTopN) || treeTopN < 0)
            {
                return false;
            }

            if (!int.TryParse(parts[heuristicStartIndex + 1], out var rolloutTopN) || rolloutTopN < 0)
            {
                return false;
            }

            heuristicOptions = new MonteCarloHeuristicOptions
            {
                Enabled = treeTopN > 0 || rolloutTopN > 0,
                TreeTopN = treeTopN,
                RolloutTopN = rolloutTopN
            };

            return true;
        }

        private static HaggisAction ResolveReplayAction(RoundState state, string selectedAction)
        {
            var legalMoves = state.PossibleActions.ToList();
            if (string.Equals(selectedAction, "Pass", StringComparison.Ordinal))
            {
                var passAction = legalMoves.FirstOrDefault(move => move.IsPass);
                if (passAction == null)
                {
                    throw new InvalidOperationException(
                        $"Replay action 'Pass' is not legal at round={state.RoundNumber} move={state.MoveIteration + 1}.");
                }

                return passAction;
            }

            var matchingAction = legalMoves.FirstOrDefault(move =>
                !move.IsPass &&
                string.Equals(move.Desc, selectedAction, StringComparison.Ordinal));
            if (matchingAction == null)
            {
                throw new InvalidOperationException(
                    $"Replay action '{selectedAction}' not found in legal moves at round={state.RoundNumber} move={state.MoveIteration + 1}.");
            }

            return matchingAction;
        }

        private static HeuristicRankingSnapshot BuildHeuristicRanking(
            MctsRunAnalyzerOptions options,
            RoundState state,
            string playerName)
        {
            var strategyName = StrategyForSeat(options, playerName);
            if (string.IsNullOrWhiteSpace(strategyName))
            {
                return null;
            }

            var parts = strategyName.Split(':');
            if (parts.Length < 5 || !string.Equals(parts[0], "montecarlo", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!TryParseTreeTopN(parts, out var treeTopN) || treeTopN <= 0)
            {
                return null;
            }

            var actions = state.PossibleActions?.ToList() ?? new List<HaggisAction>();
            var ranker = HeuristicActionRanker.CreateDefault(new HeuristicOptions());
            var ranked = state.CurrentTrickPlay?.LastAction == null
                ? ranker.RankOpeningActions(state, actions)
                : ranker.RankContinuationActions(state, actions);

            return new HeuristicRankingSnapshot
            {
                TreeTopN = treeTopN,
                RankedActions = ranked.ToList()
            };
        }

        private static bool TryParseTreeTopN(string[] parts, out int treeTopN)
        {
            treeTopN = 0;
            var startIndex = parts.Length >= 6 ? 4 : 3;
            return parts.Length > startIndex && int.TryParse(parts[startIndex], out treeTopN);
        }

        private static ReplayLog ParseReplayLog(string path)
        {
            var lines = File.ReadAllLines(path);
            var moves = new List<ReplayMove>();
            var playerNames = new List<string>();
            ReplayMove currentMove = null;
            int? seed = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine ?? string.Empty;
                if (line.StartsWith("GAME ", StringComparison.Ordinal))
                {
                    var seedToken = line.Split(' ').FirstOrDefault(token => token.StartsWith("seed=", StringComparison.Ordinal));
                    if (seedToken != null && int.TryParse(seedToken.Substring("seed=".Length), out var parsedSeed))
                    {
                        seed = parsedSeed;
                    }

                    continue;
                }

                if (line.StartsWith("MOVE ", StringComparison.Ordinal))
                {
                    currentMove = ParseReplayMove(line);
                    if (!playerNames.Contains(currentMove.PlayerName))
                    {
                        playerNames.Add(currentMove.PlayerName);
                    }

                    continue;
                }

                var trimmed = line.TrimStart();
                if (currentMove != null && trimmed.StartsWith("selected-action:", StringComparison.Ordinal))
                {
                    currentMove.SelectedAction = trimmed.Substring("selected-action:".Length).Trim();
                    moves.Add(currentMove);
                    currentMove = null;
                }
            }

            if (moves.Count == 0)
            {
                throw new InvalidOperationException($"No replay moves found in log: {path}");
            }

            return new ReplayLog(seed, playerNames, moves);
        }

        private static ReplayMove ParseReplayMove(string line)
        {
            var roundToken = line.Split(' ').FirstOrDefault(token => token.StartsWith("round=", StringComparison.Ordinal));
            var moveToken = line.Split(' ').FirstOrDefault(token => token.StartsWith("move=", StringComparison.Ordinal));
            var playerToken = line.Split(' ').FirstOrDefault(token => token.StartsWith("player=", StringComparison.Ordinal));
            var strategyToken = line.Split(' ').FirstOrDefault(token => token.StartsWith("strategy=", StringComparison.Ordinal));

            if (roundToken == null || moveToken == null || playerToken == null)
            {
                throw new InvalidOperationException($"Invalid replay MOVE line: {line}");
            }

            return new ReplayMove(
                int.Parse(roundToken.Substring("round=".Length)),
                long.Parse(moveToken.Substring("move=".Length)),
                playerToken.Substring("player=".Length),
                strategyToken == null ? null : strategyToken.Substring("strategy=".Length),
                null);
        }
    }

    internal sealed class ReplayLog
    {
        public ReplayLog(int? seed, IReadOnlyList<string> playerNames, IReadOnlyList<ReplayMove> moves)
        {
            Seed = seed;
            PlayerNames = playerNames;
            Moves = moves;
        }

        public int? Seed { get; }
        public IReadOnlyList<string> PlayerNames { get; }
        public IReadOnlyList<ReplayMove> Moves { get; }

        public int DecisionNumber(int roundNumber, long moveNumber)
        {
            var index = Moves.ToList().FindIndex(move => move.RoundNumber == roundNumber && move.MoveNumber == moveNumber);
            return index < 0 ? 1 : index + 1;
        }
    }

    internal sealed class ReplayMove
    {
        public ReplayMove(int roundNumber, long moveNumber, string playerName, string strategyName, string selectedAction)
        {
            RoundNumber = roundNumber;
            MoveNumber = moveNumber;
            PlayerName = playerName;
            StrategyName = strategyName;
            SelectedAction = selectedAction;
        }

        public int RoundNumber { get; }
        public long MoveNumber { get; }
        public string PlayerName { get; }
        public string StrategyName { get; }
        public string SelectedAction { get; set; }
    }

    public sealed class MctsRunAnalyzerResult
    {
        public string Context { get; set; }
        public string TargetPlayer { get; set; }
        public string TargetStrategy { get; set; }
        public string ChosenAction { get; set; }
        public IReadOnlyList<string> SetupLines { get; set; }
        public HeuristicRankingSnapshot HeuristicRanking { get; set; }
        public IReadOnlyList<MctsTraceEvent> TraceEvents { get; set; }
        public MonteCarloResult ComputedResult { get; set; }
    }

    public sealed class HeuristicRankingSnapshot
    {
        public int TreeTopN { get; set; }
        public IReadOnlyList<HeuristicRankedAction> RankedActions { get; set; }
    }
}
