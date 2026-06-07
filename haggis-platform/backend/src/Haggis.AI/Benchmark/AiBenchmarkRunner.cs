using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using Haggis.AI.Model;
using Haggis.AI.Strategies;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using MonteCarlo;

namespace Haggis.AI.Benchmark
{
    public sealed class AiBenchmarkRunner
    {
        public IReadOnlyList<AiBenchmarkGameResult> Run(AiBenchmarkOptions options)
        {
            var results = new List<AiBenchmarkGameResult>();
            var rotations = options.Rotate ? options.Players : 1;

            for (var seedOffset = 0; seedOffset < options.Games; seedOffset++)
            {
                var seed = options.SeedStart + seedOffset;
                for (var rotation = 0; rotation < rotations; rotation++)
                {
                    results.Add(RunSafely(options, seed, rotation));
                }
            }

            return results;
        }

        private static AiBenchmarkGameResult RunSafely(AiBenchmarkOptions options, int seed, int rotation)
        {
            var logLines = new List<string>();
            try
            {
                return RunOneGame(options, seed, rotation, logLines);
            }
            catch (Exception exception)
            {
                logLines.Add($"GAME FAILED seed={seed} rotation={rotation}");
                logLines.Add(exception.ToString());
                return new AiBenchmarkGameResult
                {
                    Seed = seed,
                    Rotation = rotation,
                    Completed = false,
                    Error = exception.ToString(),
                    LogLines = logLines
                };
            }
        }

        private static AiBenchmarkGameResult RunOneGame(
            AiBenchmarkOptions options,
            int seed,
            int rotation,
            List<string> logLines)
        {
            var gameTimer = Stopwatch.StartNew();
            var aggregatedTiming = new MctsTimingResult();
            var strategiesByPlayer = BuildStrategiesByPlayer(options, rotation);
            var players = strategiesByPlayer
                .Select(item => (IHaggisPlayer)CreatePlayer(item.Key, item.Value, logLines, aggregatedTiming))
                .ToList();

            var game = new HaggisGame(
                players,
                new ClassicHaggisScoringStrategy(gameOverScore: options.GameOverScore));
            game.SetSeed(seed);

            var state = game.NewRound();
            SetInitialRoundStartingPlayer(state, rotation);
            var moves = 0;
            var trickNumber = 0;
            LogGameStart(logLines, seed, rotation, strategiesByPlayer);
            LogRoundStart(logLines, state, game.ScoringTable.GetPlayersTotalPoints());
            while (!game.GameOver())
            {
                while (!state.RoundOver())
                {
                    if (++moves > options.MaxMovesPerGame)
                    {
                        throw new InvalidOperationException(
                            $"Game exceeded max move guard ({options.MaxMovesPerGame}).");
                    }

                    if (!(state.CurrentPlayer is AIPlayer aiPlayer))
                    {
                        throw new InvalidOperationException("Benchmark supports AI players only.");
                    }

                    if (aiPlayer.PlayStrategy is MonteCarloStrategy monteCarloStrategy)
                    {
                        monteCarloStrategy.TraceContext =
                            $"seed={seed} rotation={rotation} round={state.RoundNumber} move={moves + 1} player={state.CurrentPlayer.Name}";
                    }

                    var action = aiPlayer.GetPlayingAction(state);
                    if (state.CurrentTrickPlay.Actions.Count == 0)
                    {
                        trickNumber++;
                        LogTrickStart(logLines, state, trickNumber);
                    }

                    LogMove(logLines, moves, state, action);
                    state.ApplyAction(action);
                }

                game.RegisterRoundScoringResult(state);
                LogRoundEnd(logLines, state, game.ScoringTable.GetPlayersTotalPoints());
                if (!game.GameOver())
                {
                    state = game.NewRound();
                    trickNumber = 0;
                    LogRoundStart(logLines, state, game.ScoringTable.GetPlayersTotalPoints());
                }
            }

            var scores = game.ScoringTable.GetPlayersTotalPoints()
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            var winner = scores
                .OrderByDescending(item => item.Value)
                .ThenBy(item => SeatNumber(item.Key))
                .First();

            var result = new AiBenchmarkGameResult
            {
                Seed = seed,
                Rotation = rotation,
                Completed = true,
                Winner = winner.Key,
                WinnerStrategy = strategiesByPlayer[winner.Key],
                WinnerSeat = SeatNumber(winner.Key),
                Rounds = game.ScoringTable.Count,
                Moves = moves,
                GameElapsedMs = gameTimer.ElapsedMilliseconds,
                LogLines = logLines,
                Scores = scores,
                StrategiesByPlayer = strategiesByPlayer,
                Timing = HasTiming(aggregatedTiming) ? aggregatedTiming : null
            };
            LogGameEnd(logLines, winner.Key, strategiesByPlayer[winner.Key], scores, moves, game.ScoringTable.Count, result.GameElapsedMs);
            return result;
        }

        private static AIPlayer CreatePlayer(
            string playerName,
            string strategyName,
            List<string> logLines,
            MctsTimingResult aggregatedTiming)
        {
            var strategy = AiBenchmarkStrategyFactory.Create(strategyName);
            if (strategy is MonteCarloStrategy monteCarloStrategy)
            {
                monteCarloStrategy.CaptureTiming = true;
                monteCarloStrategy.OnComputed += result => LogMonteCarloResult(logLines, result, aggregatedTiming);
            }

            return new AIPlayer(playerName, strategy);
        }

        private static void LogMonteCarloResult(
            List<string> logLines,
            MonteCarloResult result,
            MctsTimingResult aggregatedTiming)
        {
            if (result == null)
            {
                return;
            }

            logLines.Add(
                $"    mcts: player={result.Player?.Name} iterations={result.Iterations} budgetMs={result.BudgetMs} elapsedMs={result.ElapsedMs} workers={result.Workers} legalActions={result.LegalActionsCount} rootChildren={result.RootChildrenCount} scheduledRollouts={result.ScheduledRollouts} completedRollouts={result.CompletedRollouts}");

            if (result.Timing != null)
            {
                logLines.Add($"      timing: {FormatTiming(result.Timing)}");
                AccumulateTiming(aggregatedTiming, result.Timing);
            }

            var actions = result.Actions ?? new List<MonteCarloActionInfo>();
            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index];
                var winRate = (action.WinRate * 100).ToString("0.0", CultureInfo.InvariantCulture);
                var wins = action.NumWins.ToString("0.###", CultureInfo.InvariantCulture);
                logLines.Add(
                    $"      {index + 1}. action={action.Action?.Desc} runs={action.NumRuns} wins={wins} winRate={winRate}%");
            }
        }

        private static string FormatTiming(MctsTimingResult timing)
        {
            return string.Join(", ", new[]
            {
                $"completedRollouts={timing.CompletedRollouts}",
                $"searchMs={timing.SearchMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"searchMsPerIteration={timing.SearchMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"schedulerMs={timing.SchedulerMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"schedulerMsPerIteration={timing.SchedulerMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"cloneStateMs={timing.CloneStateMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"cloneStateMsPerIteration={timing.CloneStateMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationMs={timing.MoveGenerationMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationMsPerIteration={timing.MoveGenerationMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationTreeMs={timing.MoveGenerationTreeMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationTreeMsPerIteration={timing.MoveGenerationTreeMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationRolloutMs={timing.MoveGenerationRolloutMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationRolloutMsPerIteration={timing.MoveGenerationRolloutMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationHandIndexMs={timing.MoveGenerationHandIndexMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationHandIndexMsPerIteration={timing.MoveGenerationHandIndexMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationSameCardsMs={timing.MoveGenerationSameCardsMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationSameCardsMsPerIteration={timing.MoveGenerationSameCardsMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationSameCardsWithWildsMs={timing.MoveGenerationSameCardsWithWildsMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationSameCardsWithWildsMsPerIteration={timing.MoveGenerationSameCardsWithWildsMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationSequencesMs={timing.MoveGenerationSequencesMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationSequencesMsPerIteration={timing.MoveGenerationSequencesMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationStairsMs={timing.MoveGenerationStairsMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationStairsMsPerIteration={timing.MoveGenerationStairsMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationBombsMs={timing.MoveGenerationBombsMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationBombsMsPerIteration={timing.MoveGenerationBombsMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationContinuationFilterMs={timing.MoveGenerationContinuationFilterMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationContinuationFilterMsPerIteration={timing.MoveGenerationContinuationFilterMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationTrickSelectionMs={timing.MoveGenerationTrickSelectionMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationTrickSelectionMsPerIteration={timing.MoveGenerationTrickSelectionMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationActionWrappingMs={timing.MoveGenerationActionWrappingMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationActionWrappingMsPerIteration={timing.MoveGenerationActionWrappingMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationActionSelectionMs={timing.MoveGenerationActionSelectionMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationActionSelectionMsPerIteration={timing.MoveGenerationActionSelectionMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"moveGenerationPassAppendMs={timing.MoveGenerationPassAppendMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"moveGenerationPassAppendMsPerIteration={timing.MoveGenerationPassAppendMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"selectionMs={timing.SelectionMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"selectionMsPerIteration={timing.SelectionMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"expansionMs={timing.ExpansionMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"expansionMsPerIteration={timing.ExpansionMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"rolloutMs={timing.RolloutMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"rolloutMsPerIteration={timing.RolloutMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}",
                $"backpropagationMs={timing.BackpropagationMs.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"backpropagationMsPerIteration={timing.BackpropagationMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)}"
            });
        }

        private static void AccumulateTiming(MctsTimingResult target, MctsTimingResult source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.ScheduledRollouts += source.ScheduledRollouts;
            target.CompletedRollouts += source.CompletedRollouts;
            target.SearchMs += source.SearchMs;
            target.SchedulerMs += source.SchedulerMs;
            target.CloneStateMs += source.CloneStateMs;
            target.MoveGenerationMs += source.MoveGenerationMs;
            target.MoveGenerationTreeMs += source.MoveGenerationTreeMs;
            target.MoveGenerationRolloutMs += source.MoveGenerationRolloutMs;
            target.MoveGenerationHandIndexMs += source.MoveGenerationHandIndexMs;
            target.MoveGenerationSameCardsMs += source.MoveGenerationSameCardsMs;
            target.MoveGenerationSameCardsWithWildsMs += source.MoveGenerationSameCardsWithWildsMs;
            target.MoveGenerationSequencesMs += source.MoveGenerationSequencesMs;
            target.MoveGenerationStairsMs += source.MoveGenerationStairsMs;
            target.MoveGenerationBombsMs += source.MoveGenerationBombsMs;
            target.MoveGenerationContinuationFilterMs += source.MoveGenerationContinuationFilterMs;
            target.MoveGenerationTrickSelectionMs += source.MoveGenerationTrickSelectionMs;
            target.MoveGenerationActionWrappingMs += source.MoveGenerationActionWrappingMs;
            target.MoveGenerationActionSelectionMs += source.MoveGenerationActionSelectionMs;
            target.MoveGenerationPassAppendMs += source.MoveGenerationPassAppendMs;
            target.SelectionMs += source.SelectionMs;
            target.ExpansionMs += source.ExpansionMs;
            target.RolloutMs += source.RolloutMs;
            target.BackpropagationMs += source.BackpropagationMs;
        }

        private static bool HasTiming(MctsTimingResult timing)
        {
            return timing != null &&
                   (timing.SearchMs > 0 ||
                    timing.SchedulerMs > 0 ||
                    timing.CloneStateMs > 0 ||
                    timing.MoveGenerationMs > 0 ||
                    timing.MoveGenerationTreeMs > 0 ||
                    timing.MoveGenerationRolloutMs > 0 ||
                    timing.MoveGenerationHandIndexMs > 0 ||
                    timing.MoveGenerationSameCardsMs > 0 ||
                    timing.MoveGenerationSameCardsWithWildsMs > 0 ||
                    timing.MoveGenerationSequencesMs > 0 ||
                    timing.MoveGenerationStairsMs > 0 ||
                    timing.MoveGenerationBombsMs > 0 ||
                    timing.MoveGenerationContinuationFilterMs > 0 ||
                    timing.MoveGenerationTrickSelectionMs > 0 ||
                    timing.MoveGenerationActionWrappingMs > 0 ||
                    timing.MoveGenerationActionSelectionMs > 0 ||
                    timing.MoveGenerationPassAppendMs > 0 ||
                    timing.SelectionMs > 0 ||
                    timing.ExpansionMs > 0 ||
                    timing.RolloutMs > 0 ||
                    timing.BackpropagationMs > 0);
        }

        private static void SetInitialRoundStartingPlayer(RoundState state, int rotation)
        {
            if (state.RoundNumber != 1 || state.Players.Count == 0)
            {
                return;
            }

            var startIndex = Math.Abs(rotation) % state.Players.Count;
            state.SetCurrentPlayer(state.Players[startIndex]);
        }

        private static void LogGameStart(
            List<string> logLines,
            int seed,
            int rotation,
            IReadOnlyDictionary<string, string> strategiesByPlayer)
        {
            logLines.Add($"GAME seed={seed} rotation={rotation}");
            logLines.Add("  players:");
            foreach (var item in strategiesByPlayer.OrderBy(item => SeatNumber(item.Key)))
            {
                logLines.Add($"    {item.Key}: {item.Value}");
            }
        }

        private static void LogRoundStart(
            List<string> logLines,
            RoundState state,
            IReadOnlyDictionary<string, int> totals)
        {
            logLines.Add($"  ROUND {state.RoundNumber} START current={state.CurrentPlayer.Name}");
            logLines.Add($"    totals: {FormatScores(totals)}");
            foreach (var player in state.Players.OrderBy(player => SeatNumber(player.Name)))
            {
                logLines.Add($"    hand {player.Name}: {FormatCards(player.Hand)}");
            }
            logLines.Add($"    haggis: {FormatCards(state.HaggisCards)}");
        }

        private static void LogMove(
            List<string> logLines,
            int moveNumber,
            RoundState state,
            HaggisAction action)
        {
            logLines.Add(
                $"    move {moveNumber}: player={state.CurrentPlayer.Name} action={action.Desc}");
        }

        private static void LogTrickStart(
            List<string> logLines,
            RoundState state,
            int trickNumber)
        {
            logLines.Add($"    TRICK {trickNumber} START current={state.CurrentPlayer.Name}");
            foreach (var player in state.Players.OrderBy(player => SeatNumber(player.Name)))
            {
                logLines.Add($"      hand {player.Name}: {FormatCards(player.Hand)}");
            }
            logLines.Add($"      haggis: {FormatCards(state.HaggisCards)}");
        }

        private static void LogRoundEnd(
            List<string> logLines,
            RoundState state,
            IReadOnlyDictionary<string, int> totals)
        {
            var finishingOrder = state.FinishingOrder
                .Select(playerGuid => state.Players.FirstOrDefault(player => player.GUID == playerGuid)?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name));
            logLines.Add($"  ROUND {state.RoundNumber} END");
            logLines.Add($"    finishing-order: {string.Join(", ", finishingOrder)}");
            logLines.Add($"    round-scores: {string.Join(", ", state.Players.OrderBy(player => SeatNumber(player.Name)).Select(player => $"{player.Name}={player.Score}"))}");
            logLines.Add($"    totals: {FormatScores(totals)}");
        }

        private static void LogGameEnd(
            List<string> logLines,
            string winner,
            string winnerStrategy,
            IReadOnlyDictionary<string, int> scores,
            int moves,
            int rounds,
            long gameElapsedMs)
        {
            logLines.Add($"  GAME END winner={winner} strategy={winnerStrategy} rounds={rounds} moves={moves} gameElapsedMs={gameElapsedMs}");
            logLines.Add($"    final-scores: {FormatScores(scores)}");
        }

        private static string FormatScores(IReadOnlyDictionary<string, int> scores)
        {
            if (scores == null || scores.Count == 0)
            {
                return "(none)";
            }

            return string.Join(", ", scores.OrderBy(item => SeatNumber(item.Key)).Select(item => $"{item.Key}={item.Value}"));
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

        private static Dictionary<string, string> BuildStrategiesByPlayer(AiBenchmarkOptions options, int rotation)
        {
            var strategies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < options.Players; index++)
            {
                var playerName = $"p{index + 1}";
                strategies[playerName] = options.GetSeatStrategy(index + 1);
            }

            return strategies;
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
}
