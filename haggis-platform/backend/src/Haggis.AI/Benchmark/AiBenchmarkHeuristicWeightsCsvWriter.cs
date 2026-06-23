using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Haggis.AI.Benchmark
{
    public static class AiBenchmarkHeuristicWeightsCsvWriter
    {
        public static void Write(string path, AiBenchmarkOptions options, IEnumerable<AiBenchmarkGameResult> results)
        {
            var benchmarkResults = (results ?? new List<AiBenchmarkGameResult>()).ToList();
            var completedGames = benchmarkResults.Count(result => result.Completed);
            var lines = new List<string>
            {
                "seat,player,strategy,heuristicWeights,wins,winRatePct,averageScore"
            };

            for (var seat = 1; seat <= options.Players; seat++)
            {
                var playerName = $"p{seat}";
                var wins = benchmarkResults.Count(result =>
                    result.Completed &&
                    string.Equals(result.Winner, playerName, System.StringComparison.OrdinalIgnoreCase));
                var winRate = completedGames == 0 ? 0d : wins * 100d / completedGames;
                var scores = benchmarkResults
                    .Where(result => result.Completed)
                    .Select(result => result.Scores.TryGetValue(playerName, out var score) ? (int?)score : null)
                    .Where(score => score.HasValue)
                    .Select(score => score.Value)
                    .ToList();
                var averageScore = scores.Count == 0 ? 0d : scores.Average();

                lines.Add(string.Join(",",
                    seat,
                    playerName,
                    Escape(options.GetSeatStrategy(seat)),
                    Escape(HeuristicOptionsSerializer.Serialize(options.GetSeatHeuristicOptions(seat))),
                    wins,
                    winRate.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    averageScore.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllLines(path, lines, Encoding.UTF8);
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
