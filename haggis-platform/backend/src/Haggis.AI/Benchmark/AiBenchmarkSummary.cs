using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Haggis.AI.Benchmark
{
    public sealed class AiBenchmarkSummary
    {
        public AiBenchmarkSummary(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            Results = results ?? Array.Empty<AiBenchmarkGameResult>();
            TotalGames = Results.Count;
            CompletedGames = Results.Count(result => result.Completed);
            FailedGames = TotalGames - CompletedGames;
            AverageRounds = CompletedGames == 0 ? 0 : Results.Where(r => r.Completed).Average(r => r.Rounds);
            AverageMoves = CompletedGames == 0 ? 0 : Results.Where(r => r.Completed).Average(r => r.Moves);
            AverageFinalScore = CalculateAverageFinalScore(Results);
            WinRateByStrategy = CalculateStrategyWinRates(Results);
            WinRateBySeat = CalculateSeatWinRates(Results);
        }

        public IReadOnlyList<AiBenchmarkGameResult> Results { get; }
        public int TotalGames { get; }
        public int CompletedGames { get; }
        public int FailedGames { get; }
        public double AverageRounds { get; }
        public double AverageMoves { get; }
        public double AverageFinalScore { get; }
        public IReadOnlyDictionary<string, double> WinRateByStrategy { get; }
        public IReadOnlyDictionary<int, double> WinRateBySeat { get; }

        public string Format()
        {
            var lines = new List<string>
            {
                "Haggis AI benchmark",
                "===================",
                $"Games: {CompletedGames}/{TotalGames} completed",
                $"Errors/timeouts: {FailedGames}",
                $"Average rounds: {AverageRounds.ToString("0.00", CultureInfo.InvariantCulture)}",
                $"Average moves: {AverageMoves.ToString("0.00", CultureInfo.InvariantCulture)}",
                $"Average final score: {AverageFinalScore.ToString("0.00", CultureInfo.InvariantCulture)}",
                string.Empty,
                "Win rate by strategy:"
            };

            foreach (var item in WinRateByStrategy.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                lines.Add($"  {item.Key}: {item.Value.ToString("0.00", CultureInfo.InvariantCulture)}%");
            }

            lines.Add(string.Empty);
            lines.Add("Win rate by seat:");
            foreach (var item in WinRateBySeat.OrderBy(item => item.Key))
            {
                lines.Add($"  p{item.Key}: {item.Value.ToString("0.00", CultureInfo.InvariantCulture)}%");
            }

            if (FailedGames > 0)
            {
                lines.Add(string.Empty);
                lines.Add("Failures:");
                foreach (var result in Results.Where(result => !result.Completed).Take(10))
                {
                    lines.Add($"  seed={result.Seed}, rotation={result.Rotation}: {result.Error}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static IReadOnlyDictionary<string, double> CalculateStrategyWinRates(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            var completed = results.Where(result => result.Completed).ToList();
            if (completed.Count == 0)
            {
                return new Dictionary<string, double>();
            }

            return completed
                .GroupBy(result => result.WinnerStrategy ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count() * 100.0 / completed.Count,
                    StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<int, double> CalculateSeatWinRates(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            var completed = results.Where(result => result.Completed).ToList();
            if (completed.Count == 0)
            {
                return new Dictionary<int, double>();
            }

            return completed
                .GroupBy(result => result.WinnerSeat)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count() * 100.0 / completed.Count);
        }

        private static double CalculateAverageFinalScore(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            var scores = results
                .Where(result => result.Completed)
                .SelectMany(result => result.Scores.Values)
                .ToList();

            return scores.Count == 0 ? 0 : scores.Average();
        }
    }
}
