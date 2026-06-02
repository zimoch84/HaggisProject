using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Haggis.AI.Benchmark
{
    public static class AiBenchmarkCsvWriter
    {
        public static void Write(string path, IEnumerable<AiBenchmarkGameResult> results)
        {
            var lines = new List<string>
            {
                "seed,rotation,winner,winnerStrategy,rounds,moves,gameElapsedMs,p1Score,p2Score,p3Score,mctsCompletedRollouts,mctsSearchMs,mctsSearchMsPerIteration,mctsSchedulerMs,mctsSchedulerMsPerIteration,mctsCloneStateMs,mctsCloneStateMsPerIteration,mctsMoveGenerationMs,mctsMoveGenerationMsPerIteration,mctsSelectionMs,mctsSelectionMsPerIteration,mctsExpansionMs,mctsExpansionMsPerIteration,mctsRolloutMs,mctsRolloutMsPerIteration,mctsBackpropagationMs,mctsBackpropagationMsPerIteration"
            };

            foreach (var result in results)
            {
                lines.Add(string.Join(",",
                    result.Seed,
                    result.Rotation,
                    Escape(result.Winner),
                    Escape(result.WinnerStrategy),
                    result.Rounds,
                    result.Moves,
                    result.GameElapsedMs,
                    Score(result, "p1"),
                    Score(result, "p2"),
                    Score(result, "p3"),
                    TimingInt(result, timing => timing.CompletedRollouts),
                    Timing(result, timing => timing.SearchMs),
                    Timing(result, timing => timing.SearchMsPerIteration),
                    Timing(result, timing => timing.SchedulerMs),
                    Timing(result, timing => timing.SchedulerMsPerIteration),
                    Timing(result, timing => timing.CloneStateMs),
                    Timing(result, timing => timing.CloneStateMsPerIteration),
                    Timing(result, timing => timing.MoveGenerationMs),
                    Timing(result, timing => timing.MoveGenerationMsPerIteration),
                    Timing(result, timing => timing.SelectionMs),
                    Timing(result, timing => timing.SelectionMsPerIteration),
                    Timing(result, timing => timing.ExpansionMs),
                    Timing(result, timing => timing.ExpansionMsPerIteration),
                    Timing(result, timing => timing.RolloutMs),
                    Timing(result, timing => timing.RolloutMsPerIteration),
                    Timing(result, timing => timing.BackpropagationMs),
                    Timing(result, timing => timing.BackpropagationMsPerIteration)));
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        private static int? Score(AiBenchmarkGameResult result, string playerName)
        {
            return result.Scores.TryGetValue(playerName, out var score) ? score : (int?)null;
        }

        private static string Timing(AiBenchmarkGameResult result, System.Func<MonteCarlo.MctsTimingResult, double> selector)
        {
            if (result?.Timing == null)
            {
                return string.Empty;
            }

            return selector(result.Timing).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string TimingInt(AiBenchmarkGameResult result, System.Func<MonteCarlo.MctsTimingResult, int> selector)
        {
            if (result?.Timing == null)
            {
                return string.Empty;
            }

            return selector(result.Timing).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (!value.Contains(",") && !value.Contains("\"") && !value.Contains("\n"))
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
