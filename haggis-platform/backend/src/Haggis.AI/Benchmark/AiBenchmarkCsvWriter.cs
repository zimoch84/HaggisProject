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
                "seed,rotation,winner,winnerStrategy,rounds,moves,p1Score,p2Score,p3Score"
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
                    Score(result, "p1"),
                    Score(result, "p2"),
                    Score(result, "p3")));
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
