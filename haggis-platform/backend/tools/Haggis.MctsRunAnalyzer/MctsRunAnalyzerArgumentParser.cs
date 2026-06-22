using System;
using System.Collections.Generic;
using Haggis.AI.Benchmark;

namespace Haggis.MctsRunAnalyzer
{
    public static class MctsRunAnalyzerArgumentParser
    {
        public static MctsRunAnalyzerOptions Parse(string[] args)
        {
            var options = new MctsRunAnalyzerOptions();
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
                "--seed=1",
                "--ai1=montecarlo",
                "--ai2=normal",
                "--ai3=normal",
                "--iterations=1000",
                "--timebudget=1000",
                "--workers=1",
                "--output=path"
            };
        }

        public static IReadOnlyCollection<string> SupportedStrategies =>
            new[]
            {
                "normal",
                "random",
                "montecarlo",
                "montecarlo-fast",
                "montecarlo:<simulations>:<budget-ms>[:workers]"
            };

        private static void Apply(MctsRunAnalyzerOptions options, string key, string value)
        {
            switch (key.ToLowerInvariant())
            {
                case "seed":
                    options.Seed = ParseInt(key, value);
                    break;
                case "ai1":
                    options.Ai1Strategy = value;
                    break;
                case "ai2":
                    options.Ai2Strategy = value;
                    break;
                case "ai3":
                    options.Ai3Strategy = value;
                    break;
                case "iterations":
                    options.Iterations = ParseInt(key, value);
                    break;
                case "timebudget":
                    options.TimeBudgetMs = ParseLong(key, value);
                    break;
                case "workers":
                    options.Workers = ParseInt(key, value);
                    break;
                case "output":
                    options.OutputPath = value;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '--{key}'.");
            }
        }

        private static void Validate(MctsRunAnalyzerOptions options)
        {
            if (options.Iterations <= 0)
            {
                throw new ArgumentException("--iterations must be greater than zero.");
            }

            if (options.TimeBudgetMs <= 0)
            {
                throw new ArgumentException("--timebudget must be greater than zero.");
            }

            if (options.Workers <= 0)
            {
                throw new ArgumentException("--workers must be greater than zero.");
            }

            EnsureSupported(options.Ai1Strategy);
            EnsureSupported(options.Ai2Strategy);
            EnsureSupported(options.Ai3Strategy);
        }

        private static void EnsureSupported(string strategy)
        {
            if (string.Equals(strategy, "montecarlo", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AiBenchmarkStrategyFactory.EnsureSupported(strategy);
        }

        private static int ParseInt(string key, string value)
        {
            if (!int.TryParse(value, out var parsed))
            {
                throw new ArgumentException($"--{key} must be an integer.");
            }

            return parsed;
        }

        private static long ParseLong(string key, string value)
        {
            if (!long.TryParse(value, out var parsed))
            {
                throw new ArgumentException($"--{key} must be an integer.");
            }

            return parsed;
        }
    }
}
