using System;
using System.Collections.Generic;

namespace Haggis.AI.Benchmark
{
    public static class AiBenchmarkArgumentParser
    {
        public static AiBenchmarkOptions Parse(string[] args)
        {
            var options = new AiBenchmarkOptions();
            foreach (var argument in args ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(argument) || !argument.StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                var separatorIndex = argument.IndexOf('=');
                var key = separatorIndex < 0
                    ? argument.Substring(2)
                    : argument.Substring(2, separatorIndex - 2);
                var value = separatorIndex < 0
                    ? "true"
                    : argument.Substring(separatorIndex + 1);

                Apply(options, key.Trim(), value.Trim());
            }

            Validate(options);
            return options;
        }

        public static IReadOnlyList<string> SupportedArguments()
        {
            return new[]
            {
                "--games=1000",
                "--players=3",
                "--seed-start=1",
                "--game-over-score=250",
                "--ai1=normal",
                "--ai1-weights=ContinuationFollowUpWeight=0.5;BombOpeningWeight=0",
                "--ai2=montecarlo:800:100:4",
                "--ai2=montecarlo:800:100:4:5:3",
                "--ai2-weights=ContinuationFollowUpWeight=1.25",
                "--ai3=normal",
                "--ai3-weights=ContinuationFollowUpWeight=1",
                "--rotate=true",
                "--csv=path",
                "--log=path",
                "--max-moves=10000"
            };
        }

        private static void Apply(AiBenchmarkOptions options, string key, string value)
        {
            switch (key.ToLowerInvariant())
            {
                case "games":
                    options.Games = ParseInt(key, value);
                    break;
                case "players":
                    options.Players = ParseInt(key, value);
                    break;
                case "seed-start":
                    options.SeedStart = ParseInt(key, value);
                    break;
                case "game-over-score":
                    options.GameOverScore = ParseInt(key, value);
                    break;
                case "ai1":
                    options.Ai1Strategy = value;
                    break;
                case "ai2":
                    options.Ai2Strategy = value;
                    break;
                case "ai1-weights":
                    options.Ai1HeuristicOptions = HeuristicOptionsSerializer.Parse(value);
                    break;
                case "ai2-weights":
                    options.Ai2HeuristicOptions = HeuristicOptionsSerializer.Parse(value);
                    break;
                case "ai3-weights":
                    options.Ai3HeuristicOptions = HeuristicOptionsSerializer.Parse(value);
                    break;
                case "ai3":
                    options.Ai3Strategy = value;
                    break;
                case "rotate":
                    options.Rotate = ParseBool(key, value);
                    break;
                case "csv":
                    options.CsvPath = value;
                    break;
                case "log":
                    options.LogPath = value;
                    break;
                case "max-moves":
                    options.MaxMovesPerGame = ParseInt(key, value);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '--{key}'.");
            }
        }

        private static void Validate(AiBenchmarkOptions options)
        {
            if (options.Games <= 0)
            {
                throw new ArgumentException("--games must be greater than zero.");
            }

            if (options.Players < 2 || options.Players > 3)
            {
                throw new ArgumentException("--players must be 2 or 3.");
            }

            if (options.GameOverScore <= 0)
            {
                throw new ArgumentException("--game-over-score must be greater than zero.");
            }

            if (options.MaxMovesPerGame <= 0)
            {
                throw new ArgumentException("--max-moves must be greater than zero.");
            }

            AiBenchmarkStrategyFactory.EnsureSupported(options.Ai1Strategy);
            AiBenchmarkStrategyFactory.EnsureSupported(options.Ai2Strategy);
            AiBenchmarkStrategyFactory.EnsureSupported(options.Ai3Strategy);
        }

        private static int ParseInt(string key, string value)
        {
            if (!int.TryParse(value, out var parsed))
            {
                throw new ArgumentException($"--{key} must be an integer.");
            }

            return parsed;
        }

        private static bool ParseBool(string key, string value)
        {
            if (!bool.TryParse(value, out var parsed))
            {
                throw new ArgumentException($"--{key} must be true or false.");
            }

            return parsed;
        }
    }
}
