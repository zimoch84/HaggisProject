using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Haggis.AI.Model;
using Haggis.AI.Strategies;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

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
            var strategiesByPlayer = BuildStrategiesByPlayer(options, rotation);
            var players = strategiesByPlayer
                .Select(item => (IHaggisPlayer)CreatePlayer(item.Key, item.Value, logLines))
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
                LogLines = logLines,
                Scores = scores,
                StrategiesByPlayer = strategiesByPlayer
            };
            LogGameEnd(logLines, winner.Key, strategiesByPlayer[winner.Key], scores, moves, game.ScoringTable.Count);
            return result;
        }

        private static AIPlayer CreatePlayer(
            string playerName,
            string strategyName,
            List<string> logLines)
        {
            var strategy = AiBenchmarkStrategyFactory.Create(strategyName);
            if (strategy is MonteCarloStrategy monteCarloStrategy)
            {
                monteCarloStrategy.OnComputed += result => LogMonteCarloResult(logLines, result);
            }

            return new AIPlayer(playerName, strategy);
        }

        private static void LogMonteCarloResult(
            List<string> logLines,
            MonteCarloResult result)
        {
            if (result == null)
            {
                return;
            }

            logLines.Add(
                $"    mcts: player={result.Player?.Name} iterations={result.Iterations} budgetMs={result.BudgetMs} elapsedMs={result.ElapsedMs} workers={result.Workers} legalActions={result.LegalActionsCount} rootChildren={result.RootChildrenCount} scheduledRollouts={result.ScheduledRollouts} completedRollouts={result.CompletedRollouts}");

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
            int rounds)
        {
            logLines.Add($"  GAME END winner={winner} strategy={winnerStrategy} rounds={rounds} moves={moves}");
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
