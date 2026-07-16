using System;
using System.Collections.Generic;
using System.Globalization;

namespace Haggis.AI.Benchmark
{
    public static class HeuristicGeneticTuningArgumentParser
    {
        public static HeuristicGeneticTuningOptions Parse(string[] args)
        {
            var options = new HeuristicGeneticTuningOptions();
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
                "--mode=genetic",
                "--games=100",
                "--seed-start=1",
                "--game-over-score=250",
                "--ai2=montecarlo:2000:2000:4",
                "--rotate=true",
                "--csv=results_heuristic-vs-montecarlo-2000-2000-4_100-seeds.csv",
                "--max-moves=20000",
                "--generations=5",
                "--children-per-generation=20",
                "--parent-count=10",
                "--mutation-rate=0.35",
                "--mutation-min=-3",
                "--mutation-max=3",
                "--random-seed=12345",
                "--baseline-weight=1"
            };
        }

        private static void Apply(HeuristicGeneticTuningOptions options, string key, string value)
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
                case "generations":
                    options.Generations = ParseInt(key, value);
                    break;
                case "children-per-generation":
                    options.ChildrenPerGeneration = ParseInt(key, value);
                    break;
                case "parent-count":
                    options.TopParentCount = ParseInt(key, value);
                    break;
                case "mutation-rate":
                    options.MutationRate = ParseFloat(key, value);
                    break;
                case "mutation-min":
                    options.MutationDeltaMin = ParseFloat(key, value);
                    break;
                case "mutation-max":
                    options.MutationDeltaMax = ParseFloat(key, value);
                    break;
                case "random-seed":
                    options.RandomSeed = ParseInt(key, value);
                    break;
                case "baseline-weight":
                    options.BaselineWeight = ParseFloat(key, value);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '--{key}' for genetic mode.");
            }
        }

        private static void Validate(HeuristicGeneticTuningOptions options)
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
                throw new ArgumentException("--csv is required in genetic mode.");
            }

            if (options.Generations <= 0)
            {
                throw new ArgumentException("--generations must be greater than zero.");
            }

            if (options.ChildrenPerGeneration <= 0)
            {
                throw new ArgumentException("--children-per-generation must be greater than zero.");
            }

            if (options.TopParentCount <= 0)
            {
                throw new ArgumentException("--parent-count must be greater than zero.");
            }

            if (options.MutationRate < 0f || options.MutationRate > 1f)
            {
                throw new ArgumentException("--mutation-rate must be in range 0..1.");
            }

            if (options.MutationDeltaMin > options.MutationDeltaMax)
            {
                throw new ArgumentException("--mutation-min must be less than or equal to --mutation-max.");
            }

            if (options.BaselineWeight < 0f)
            {
                throw new ArgumentException("--baseline-weight must be non-negative.");
            }

            AiBenchmarkStrategyFactory.EnsureSupported(options.MonteCarloStrategy);
            if (!options.MonteCarloStrategy.Trim().StartsWith("montecarlo", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Genetic mode currently supports only MonteCarlo as --ai2.");
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
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new ArgumentException($"--{key} must be a number.");
            }

            return parsed;
        }
    }
}
