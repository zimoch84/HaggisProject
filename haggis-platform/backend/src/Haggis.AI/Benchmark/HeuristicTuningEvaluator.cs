using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Strategies;

namespace Haggis.AI.Benchmark
{
    public sealed class HeuristicTuningEvaluator : IHeuristicTuningEvaluator
    {
        private readonly Func<AiBenchmarkOptions, IReadOnlyList<AiBenchmarkGameResult>> _runBenchmark;

        public HeuristicTuningEvaluator()
            : this(options => new AiBenchmarkRunner().Run(options))
        {
        }

        public HeuristicTuningEvaluator(Func<AiBenchmarkOptions, IReadOnlyList<AiBenchmarkGameResult>> runBenchmark)
        {
            _runBenchmark = runBenchmark ?? throw new ArgumentNullException(nameof(runBenchmark));
        }

        public HeuristicTuningRunResult Evaluate(HeuristicTuningBatchOptions batchOptions, HeuristicOptions heuristicOptions)
        {
            if (batchOptions == null)
            {
                throw new ArgumentNullException(nameof(batchOptions));
            }

            var benchmarkOptions = new AiBenchmarkOptions
            {
                Games = batchOptions.Games,
                Players = 2,
                SeedStart = batchOptions.SeedStart,
                GameOverScore = batchOptions.GameOverScore,
                Ai1Strategy = "normal",
                Ai2Strategy = batchOptions.MonteCarloStrategy,
                Rotate = batchOptions.Rotate,
                MaxMovesPerGame = batchOptions.MaxMovesPerGame,
                Ai1HeuristicOptions = heuristicOptions
            };

            var results = _runBenchmark(benchmarkOptions);
            var completedResults = results.Where(result => result.Completed).ToList();
            var heuristicScores = completedResults
                .Select(result => result.Scores.TryGetValue("p1", out var score) ? (int?)score : null)
                .Where(score => score.HasValue)
                .Select(score => score.Value)
                .ToList();
            var monteCarloScores = completedResults
                .Select(result => result.Scores.TryGetValue("p2", out var score) ? (int?)score : null)
                .Where(score => score.HasValue)
                .Select(score => score.Value)
                .ToList();
            var heuristicWins = completedResults.Count(result =>
                string.Equals(result.Winner, "p1", StringComparison.OrdinalIgnoreCase));
            var monteCarloWins = completedResults.Count(result =>
                string.Equals(result.Winner, "p2", StringComparison.OrdinalIgnoreCase));

            return new HeuristicTuningRunResult
            {
                Games = batchOptions.Games,
                SeedStart = batchOptions.SeedStart,
                Rotate = batchOptions.Rotate,
                GameOverScore = batchOptions.GameOverScore,
                MaxMovesPerGame = batchOptions.MaxMovesPerGame,
                MonteCarloStrategy = batchOptions.MonteCarloStrategy,
                HeuristicOptions = heuristicOptions,
                CompletedGames = completedResults.Count,
                FailedGames = results.Count - completedResults.Count,
                HeuristicWins = heuristicWins,
                MonteCarloWins = monteCarloWins,
                HeuristicWinRatePct = completedResults.Count == 0 ? 0d : heuristicWins * 100d / completedResults.Count,
                MonteCarloWinRatePct = completedResults.Count == 0 ? 0d : monteCarloWins * 100d / completedResults.Count,
                HeuristicAverageScore = heuristicScores.Count == 0 ? 0d : heuristicScores.Average(),
                MonteCarloAverageScore = monteCarloScores.Count == 0 ? 0d : monteCarloScores.Average(),
                AverageRounds = completedResults.Count == 0 ? 0d : completedResults.Average(result => result.Rounds),
                AverageMoves = completedResults.Count == 0 ? 0d : completedResults.Average(result => result.Moves),
                AverageGameElapsedMs = completedResults.Count == 0 ? 0d : completedResults.Average(result => result.GameElapsedMs),
                TotalGameElapsedMs = completedResults.Sum(result => result.GameElapsedMs),
                TimestampUtc = DateTime.UtcNow
            };
        }
    }
}
