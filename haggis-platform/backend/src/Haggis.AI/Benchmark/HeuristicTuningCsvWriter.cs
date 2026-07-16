using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Haggis.AI.Strategies;

namespace Haggis.AI.Benchmark
{
    public static class HeuristicTuningCsvWriter
    {
        private static readonly string[] HeaderConfigColumns =
        {
            "games",
            "seedStart",
            "rotate",
            "gameOverScore",
            "maxMovesPerGame",
            "monteCarloStrategy"
        };

        private static readonly string[] KeyConfigColumns =
        {
            "monteCarloStrategy"
        };

        private static readonly string[] OutcomeColumns =
        {
            "completedGames",
            "failedGames",
            "heuristicWins",
            "monteCarloWins",
            "heuristicWinRatePct",
            "monteCarloWinRatePct",
            "heuristicAverageScore",
            "monteCarloAverageScore",
            "averageRounds",
            "averageMoves",
            "averageGameElapsedMs",
            "totalGameElapsedMs",
            "timestampUtc"
        };

        public static IReadOnlyList<string> HeaderColumns { get; } =
            HeaderConfigColumns
                .Concat(HeuristicOptionsSerializer.WeightPropertyNames)
                .Concat(OutcomeColumns)
                .ToArray();

        public static void Append(string path, HeuristicTuningRunResult result)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("CSV path is required.", nameof(path));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            EnsureHeader(path);
            using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.WriteLine(BuildLine(result));
                writer.Flush();
            }
        }

        public static ISet<string> LoadCompletedKeys(string path)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return keys;
            }

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
            {
                return keys;
            }

            var header = ParseCsvLine(lines[0]);
            for (var index = 1; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    continue;
                }

                var values = ParseCsvLine(lines[index]);
                var row = ToRowDictionary(header, values);
                keys.Add(BuildKey(row));
            }

            return keys;
        }

        public static IReadOnlyDictionary<string, HeuristicTuningRunResult> LoadExistingResults(string path)
        {
            var results = new Dictionary<string, HeuristicTuningRunResult>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return results;
            }

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
            {
                return results;
            }

            var header = ParseCsvLine(lines[0]);
            for (var index = 1; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    continue;
                }

                var values = ParseCsvLine(lines[index]);
                var row = ToRowDictionary(header, values);
                var result = ParseRunResult(row);
                results[BuildKey(row)] = result;
            }

            return results;
        }

        public static string BuildKey(HeuristicTuningRunResult result)
        {
            var weightValues = HeuristicOptionsSerializer.ToDictionary(result.HeuristicOptions);
            var row = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["games"] = result.Games.ToString(CultureInfo.InvariantCulture),
                ["seedStart"] = result.SeedStart.ToString(CultureInfo.InvariantCulture),
                ["rotate"] = result.Rotate ? "true" : "false",
                ["gameOverScore"] = result.GameOverScore.ToString(CultureInfo.InvariantCulture),
                ["maxMovesPerGame"] = result.MaxMovesPerGame.ToString(CultureInfo.InvariantCulture),
                ["monteCarloStrategy"] = result.MonteCarloStrategy ?? string.Empty
            };

            foreach (var weightName in HeuristicOptionsSerializer.WeightPropertyNames)
            {
                row[weightName] = FormatFloat(weightValues[weightName]);
            }

            return BuildKey(row);
        }

        public static void EnsureHeader(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                File.WriteAllText(path, string.Join(",", HeaderColumns) + Environment.NewLine, new UTF8Encoding(false));
            }
        }

        private static string BuildLine(HeuristicTuningRunResult result)
        {
            var weightValues = HeuristicOptionsSerializer.ToDictionary(result.HeuristicOptions);

            return string.Join(",",
                result.Games.ToString(CultureInfo.InvariantCulture),
                result.SeedStart.ToString(CultureInfo.InvariantCulture),
                result.Rotate ? "true" : "false",
                result.GameOverScore.ToString(CultureInfo.InvariantCulture),
                result.MaxMovesPerGame.ToString(CultureInfo.InvariantCulture),
                Escape(result.MonteCarloStrategy),
                string.Join(",", HeuristicOptionsSerializer.WeightPropertyNames.Select(name => FormatFloat(weightValues[name]))),
                result.CompletedGames.ToString(CultureInfo.InvariantCulture),
                result.FailedGames.ToString(CultureInfo.InvariantCulture),
                result.HeuristicWins.ToString(CultureInfo.InvariantCulture),
                result.MonteCarloWins.ToString(CultureInfo.InvariantCulture),
                FormatDouble(result.HeuristicWinRatePct),
                FormatDouble(result.MonteCarloWinRatePct),
                FormatDouble(result.HeuristicAverageScore),
                FormatDouble(result.MonteCarloAverageScore),
                FormatDouble(result.AverageRounds),
                FormatDouble(result.AverageMoves),
                FormatDouble(result.AverageGameElapsedMs),
                result.TotalGameElapsedMs.ToString(CultureInfo.InvariantCulture),
                result.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
        }

        private static string BuildKey(IReadOnlyDictionary<string, string> row)
        {
            return string.Join("|", KeyConfigColumns
                .Concat(HeuristicOptionsSerializer.WeightPropertyNames)
                .Select(column => $"{column}={(row.TryGetValue(column, out var value) ? value : string.Empty)}"));
        }

        private static Dictionary<string, string> ToRowDictionary(IReadOnlyList<string> header, IReadOnlyList<string> values)
        {
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < header.Count; index++)
            {
                row[header[index]] = index < values.Count ? values[index] : string.Empty;
            }

            return row;
        }

        private static HeuristicTuningRunResult ParseRunResult(IReadOnlyDictionary<string, string> row)
        {
            var heuristicOptions = new HeuristicOptions();
            foreach (var weightName in HeuristicOptionsSerializer.WeightPropertyNames)
            {
                HeuristicOptionsSerializer.SetWeight(heuristicOptions, weightName, ParseFloat(row, weightName));
            }

            return new HeuristicTuningRunResult
            {
                Games = ParseInt(row, "games"),
                SeedStart = ParseInt(row, "seedStart"),
                Rotate = ParseBool(row, "rotate"),
                GameOverScore = ParseInt(row, "gameOverScore"),
                MaxMovesPerGame = ParseInt(row, "maxMovesPerGame"),
                MonteCarloStrategy = row.TryGetValue("monteCarloStrategy", out var strategy) ? strategy : string.Empty,
                HeuristicOptions = heuristicOptions,
                CompletedGames = ParseInt(row, "completedGames"),
                FailedGames = ParseInt(row, "failedGames"),
                HeuristicWins = ParseInt(row, "heuristicWins"),
                MonteCarloWins = ParseInt(row, "monteCarloWins"),
                HeuristicWinRatePct = ParseDouble(row, "heuristicWinRatePct"),
                MonteCarloWinRatePct = ParseDouble(row, "monteCarloWinRatePct"),
                HeuristicAverageScore = ParseDouble(row, "heuristicAverageScore"),
                MonteCarloAverageScore = ParseDouble(row, "monteCarloAverageScore"),
                AverageRounds = ParseDouble(row, "averageRounds"),
                AverageMoves = ParseDouble(row, "averageMoves"),
                AverageGameElapsedMs = ParseDouble(row, "averageGameElapsedMs"),
                TotalGameElapsedMs = ParseLong(row, "totalGameElapsedMs"),
                TimestampUtc = ParseDateTime(row, "timestampUtc")
            };
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            if (line == null)
            {
                return values;
            }

            var builder = new StringBuilder();
            var insideQuotes = false;
            for (var index = 0; index < line.Length; index++)
            {
                var character = line[index];
                if (insideQuotes)
                {
                    if (character == '"' && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        builder.Append('"');
                        index++;
                    }
                    else if (character == '"')
                    {
                        insideQuotes = false;
                    }
                    else
                    {
                        builder.Append(character);
                    }
                }
                else if (character == ',')
                {
                    values.Add(builder.ToString());
                    builder.Clear();
                }
                else if (character == '"')
                {
                    insideQuotes = true;
                }
                else
                {
                    builder.Append(character);
                }
            }

            values.Add(builder.ToString());
            return values;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static int ParseInt(IReadOnlyDictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
        }

        private static long ParseLong(IReadOnlyDictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var value) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0L;
        }

        private static float ParseFloat(IReadOnlyDictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0f;
        }

        private static double ParseDouble(IReadOnlyDictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0d;
        }

        private static bool ParseBool(IReadOnlyDictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) && parsed;
        }

        private static DateTime ParseDateTime(IReadOnlyDictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var value) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : DateTime.UtcNow;
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
