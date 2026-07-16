using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MonteCarlo;

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
            AverageGameElapsedMs = CompletedGames == 0 ? 0 : Results.Where(r => r.Completed).Average(r => r.GameElapsedMs);
            TotalGameElapsedMs = Results.Where(r => r.Completed).Sum(r => r.GameElapsedMs);
            AverageFinalScore = CalculateAverageFinalScore(Results);
            WinRateByStrategy = CalculateStrategyWinRates(Results);
            WinRateBySeat = CalculateSeatWinRates(Results);
            AverageTiming = CalculateAverageTiming(Results);
            AverageMctsDecisionsPerGame = CalculateAverageMctsDecisionsPerGame(Results);
            AverageTreeNodeCount = CalculateAverageTreeNodeCount(Results);
            AverageTreeDepth = CalculateAverageTreeDepth(Results);
            MaxTreeNodeCount = CalculateMaxTreeNodeCount(Results);
            MaxTreeDepth = CalculateMaxTreeDepth(Results);
        }

        public IReadOnlyList<AiBenchmarkGameResult> Results { get; }
        public int TotalGames { get; }
        public int CompletedGames { get; }
        public int FailedGames { get; }
        public double AverageRounds { get; }
        public double AverageMoves { get; }
        public double AverageGameElapsedMs { get; }
        public long TotalGameElapsedMs { get; }
        public double AverageFinalScore { get; }
        public IReadOnlyDictionary<string, double> WinRateByStrategy { get; }
        public IReadOnlyDictionary<int, double> WinRateBySeat { get; }
        public MctsTimingResult AverageTiming { get; }
        public double AverageMctsDecisionsPerGame { get; }
        public double AverageTreeNodeCount { get; }
        public double AverageTreeDepth { get; }
        public int MaxTreeNodeCount { get; }
        public int MaxTreeDepth { get; }

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
                $"Average game duration: {AverageGameElapsedMs.ToString("0.000", CultureInfo.InvariantCulture)} ms",
                $"Total simulated game time: {TotalGameElapsedMs.ToString(CultureInfo.InvariantCulture)} ms",
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

            if (AverageTiming != null)
            {
                lines.Add(string.Empty);
                lines.Add("MCTS timing (average per completed game):");
                if (AverageMctsDecisionsPerGame > 0)
                {
                    lines.Add($"  mcts decisions: {AverageMctsDecisionsPerGame.ToString("0.000", CultureInfo.InvariantCulture)}");
                    lines.Add($"  avg tree nodes: {AverageTreeNodeCount.ToString("0.000", CultureInfo.InvariantCulture)}");
                    lines.Add($"  avg tree depth: {AverageTreeDepth.ToString("0.000", CultureInfo.InvariantCulture)}");
                    lines.Add($"  max tree nodes: {MaxTreeNodeCount.ToString(CultureInfo.InvariantCulture)}");
                    lines.Add($"  max tree depth: {MaxTreeDepth.ToString(CultureInfo.InvariantCulture)}");
                }
                lines.Add($"  completed rollouts: {AverageTiming.CompletedRollouts.ToString("0.000", CultureInfo.InvariantCulture)}");
                lines.Add($"  search: {AverageTiming.SearchMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  search per iteration: {AverageTiming.SearchMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  scheduler/task overhead: {AverageTiming.SchedulerMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  scheduler/task overhead per iteration: {AverageTiming.SchedulerMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  clone state: {AverageTiming.CloneStateMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  clone state per iteration: {AverageTiming.CloneStateMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  move generation: {AverageTiming.MoveGenerationMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  move generation per iteration: {AverageTiming.MoveGenerationMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    tree: {AverageTiming.MoveGenerationTreeMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    tree per iteration: {AverageTiming.MoveGenerationTreeMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    rollout: {AverageTiming.MoveGenerationRolloutMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    rollout per iteration: {AverageTiming.MoveGenerationRolloutMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    hand index: {AverageTiming.MoveGenerationHandIndexMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    hand index per iteration: {AverageTiming.MoveGenerationHandIndexMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    same cards: {AverageTiming.MoveGenerationSameCardsMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    same cards per iteration: {AverageTiming.MoveGenerationSameCardsMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    same cards with wilds: {AverageTiming.MoveGenerationSameCardsWithWildsMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    same cards with wilds per iteration: {AverageTiming.MoveGenerationSameCardsWithWildsMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    sequences: {AverageTiming.MoveGenerationSequencesMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    sequences per iteration: {AverageTiming.MoveGenerationSequencesMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    stairs: {AverageTiming.MoveGenerationStairsMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    stairs per iteration: {AverageTiming.MoveGenerationStairsMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    bombs: {AverageTiming.MoveGenerationBombsMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    bombs per iteration: {AverageTiming.MoveGenerationBombsMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    continuation filter: {AverageTiming.MoveGenerationContinuationFilterMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    continuation filter per iteration: {AverageTiming.MoveGenerationContinuationFilterMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    trick selection: {AverageTiming.MoveGenerationTrickSelectionMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    trick selection per iteration: {AverageTiming.MoveGenerationTrickSelectionMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    action wrapping: {AverageTiming.MoveGenerationActionWrappingMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    action wrapping per iteration: {AverageTiming.MoveGenerationActionWrappingMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    action selection: {AverageTiming.MoveGenerationActionSelectionMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    action selection per iteration: {AverageTiming.MoveGenerationActionSelectionMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    pass append: {AverageTiming.MoveGenerationPassAppendMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"    pass append per iteration: {AverageTiming.MoveGenerationPassAppendMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  selection: {AverageTiming.SelectionMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  selection per iteration: {AverageTiming.SelectionMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  expansion: {AverageTiming.ExpansionMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  expansion per iteration: {AverageTiming.ExpansionMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  rollout: {AverageTiming.RolloutMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  rollout per iteration: {AverageTiming.RolloutMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  backpropagation: {AverageTiming.BackpropagationMs.ToString("0.000", CultureInfo.InvariantCulture)} ms");
                lines.Add($"  backpropagation per iteration: {AverageTiming.BackpropagationMsPerIteration.ToString("0.000000", CultureInfo.InvariantCulture)} ms");
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

        private static MctsTimingResult CalculateAverageTiming(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            var completedWithTiming = results
                .Where(result => result.Completed && result.Timing != null)
                .ToList();

            if (completedWithTiming.Count == 0)
            {
                return null;
            }

            return new MctsTimingResult
            {
                ScheduledRollouts = (int)Math.Round(completedWithTiming.Average(result => result.Timing.ScheduledRollouts)),
                CompletedRollouts = (int)Math.Round(completedWithTiming.Average(result => result.Timing.CompletedRollouts)),
                SearchMs = completedWithTiming.Average(result => result.Timing.SearchMs),
                SchedulerMs = completedWithTiming.Average(result => result.Timing.SchedulerMs),
                CloneStateMs = completedWithTiming.Average(result => result.Timing.CloneStateMs),
                MoveGenerationMs = completedWithTiming.Average(result => result.Timing.MoveGenerationMs),
                MoveGenerationTreeMs = completedWithTiming.Average(result => result.Timing.MoveGenerationTreeMs),
                MoveGenerationRolloutMs = completedWithTiming.Average(result => result.Timing.MoveGenerationRolloutMs),
                MoveGenerationHandIndexMs = completedWithTiming.Average(result => result.Timing.MoveGenerationHandIndexMs),
                MoveGenerationSameCardsMs = completedWithTiming.Average(result => result.Timing.MoveGenerationSameCardsMs),
                MoveGenerationSameCardsWithWildsMs = completedWithTiming.Average(result => result.Timing.MoveGenerationSameCardsWithWildsMs),
                MoveGenerationSequencesMs = completedWithTiming.Average(result => result.Timing.MoveGenerationSequencesMs),
                MoveGenerationStairsMs = completedWithTiming.Average(result => result.Timing.MoveGenerationStairsMs),
                MoveGenerationBombsMs = completedWithTiming.Average(result => result.Timing.MoveGenerationBombsMs),
                MoveGenerationContinuationFilterMs = completedWithTiming.Average(result => result.Timing.MoveGenerationContinuationFilterMs),
                MoveGenerationTrickSelectionMs = completedWithTiming.Average(result => result.Timing.MoveGenerationTrickSelectionMs),
                MoveGenerationActionWrappingMs = completedWithTiming.Average(result => result.Timing.MoveGenerationActionWrappingMs),
                MoveGenerationActionSelectionMs = completedWithTiming.Average(result => result.Timing.MoveGenerationActionSelectionMs),
                MoveGenerationPassAppendMs = completedWithTiming.Average(result => result.Timing.MoveGenerationPassAppendMs),
                SelectionMs = completedWithTiming.Average(result => result.Timing.SelectionMs),
                ExpansionMs = completedWithTiming.Average(result => result.Timing.ExpansionMs),
                RolloutMs = completedWithTiming.Average(result => result.Timing.RolloutMs),
                BackpropagationMs = completedWithTiming.Average(result => result.Timing.BackpropagationMs)
            };
        }

        private static double CalculateAverageMctsDecisionsPerGame(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            var completedWithDecisions = results
                .Where(result => result.Completed && result.MonteCarloDecisionCount > 0)
                .ToList();

            return completedWithDecisions.Count == 0
                ? 0
                : completedWithDecisions.Average(result => result.MonteCarloDecisionCount);
        }

        private static double CalculateAverageTreeNodeCount(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            var completedWithDecisions = results
                .Where(result => result.Completed && result.MonteCarloDecisionCount > 0)
                .ToList();

            return completedWithDecisions.Count == 0
                ? 0
                : completedWithDecisions.Average(result => result.AverageTreeNodeCount);
        }

        private static double CalculateAverageTreeDepth(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            var completedWithDecisions = results
                .Where(result => result.Completed && result.MonteCarloDecisionCount > 0)
                .ToList();

            return completedWithDecisions.Count == 0
                ? 0
                : completedWithDecisions.Average(result => result.AverageTreeDepth);
        }

        private static int CalculateMaxTreeNodeCount(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            var completedWithDecisions = results
                .Where(result => result.Completed && result.MonteCarloDecisionCount > 0)
                .ToList();

            return completedWithDecisions.Count == 0
                ? 0
                : completedWithDecisions.Max(result => result.MaxTreeNodeCount);
        }

        private static int CalculateMaxTreeDepth(IReadOnlyList<AiBenchmarkGameResult> results)
        {
            var completedWithDecisions = results
                .Where(result => result.Completed && result.MonteCarloDecisionCount > 0)
                .ToList();

            return completedWithDecisions.Count == 0
                ? 0
                : completedWithDecisions.Max(result => result.MaxTreeDepth);
        }
    }
}
