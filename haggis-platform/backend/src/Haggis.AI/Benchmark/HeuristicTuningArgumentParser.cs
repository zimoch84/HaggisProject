using System;
using System.Collections.Generic;

namespace Haggis.AI.Benchmark
{
    public static class HeuristicTuningArgumentParser
    {
        public static HeuristicTuningBatchOptions Parse(string[] args)
        {
            var options = new HeuristicTuningBatchOptions();
            var weightSets = new List<Strategies.HeuristicOptions>();

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

                Apply(options, weightSets, key.Trim(), value.Trim());
            }

            options.WeightSets = weightSets;
            Validate(options);
            return options;
        }

        public static IReadOnlyList<string> SupportedArguments()
        {
            return new[]
            {
                "--mode=tuning",
                "--games=100",
                "--seed-start=1",
                "--game-over-score=250",
                "--ai2=montecarlo:2000:2000:4",
                "--rotate=true",
                "--csv=results_heuristic-vs-montecarlo-2000-2000-4_100-seeds.csv",
                "--max-moves=20000",
                "--baseline-weight=1",
                "--weight-step=0.25",
                "--weights=ContinuationFollowUpWeight=1;BombOpeningWeight=1",
                "--weights=ContinuationFollowUpWeight=0.5;BombOpeningWeight=2"
            };
        }

        private static void Apply(
            HeuristicTuningBatchOptions options,
            List<Strategies.HeuristicOptions> weightSets,
            string key,
            string value)
        {
            switch (key.ToLowerInvariant())
            {
                case "mode":
                    break;
                case "games":
                    options.Games = ParseInt(key, value);
                    break;
                case "seed-start":
                    options.SeedStart = ParseInt(key, value);
                    break;
                case "game-over-score":
                    options.GameOverScore = ParseInt(key, value);
                    break;
                case "baseline-weight":
                    options.BaselineWeight = ParseFloat(key, value);
                    break;
                case "weight-step":
                    options.WeightStep = ParseFloat(key, value);
                    break;
                case "ai2":
                case "montecarlo":
                    options.MonteCarloStrategy = value;
                    break;
                case "rotate":
                    options.Rotate = ParseBool(key, value);
                    break;
                case "csv":
                    options.CsvPath = value;
                    break;
                case "max-moves":
                    options.MaxMovesPerGame = ParseInt(key, value);
                    break;
                case "weights":
                    weightSets.Add(HeuristicOptionsSerializer.Parse(value) ?? new Strategies.HeuristicOptions());
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '--{key}' for tuning mode.");
            }
        }

        private static void Validate(HeuristicTuningBatchOptions options)
        {
            if (options.Games <= 0)
            {
                throw new ArgumentException("--games must be greater than zero.");
            }

            if (options.GameOverScore <= 0)
            {
                throw new ArgumentException("--game-over-score must be greater than zero.");
            }

            if (options.MaxMovesPerGame <= 0)
            {
                throw new ArgumentException("--max-moves must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(options.CsvPath))
            {
                throw new ArgumentException("--csv is required in tuning mode.");
            }

            AiBenchmarkStrategyFactory.EnsureSupported(options.MonteCarloStrategy);
            if (!options.MonteCarloStrategy.Trim().StartsWith("montecarlo", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Tuning mode currently supports only MonteCarlo as --ai2.");
            }

            if (options.BaselineWeight < 0f)
            {
                throw new ArgumentException("--baseline-weight must be non-negative.");
            }

            if (options.WeightStep <= 0f)
            {
                throw new ArgumentException("--weight-step must be greater than zero.");
            }
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

        private static float ParseFloat(string key, string value)
        {
            if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                throw new ArgumentException($"--{key} must be a number.");
            }

            return parsed;
        }
    }
}
