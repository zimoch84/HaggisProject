using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.StartingTrickFilterStrategies;
using Haggis.AI.Strategies;

namespace Haggis.AI.Benchmark
{
    public static class AiBenchmarkStrategyFactory
    {
        private static readonly HashSet<string> Supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "normal",
            "heuristic-continuations",
            "random",
            "montecarlo-fast"
        };

        public static IReadOnlyCollection<string> SupportedStrategyNames =>
            Supported.Concat(new[] { "montecarlo:<simulations>:<budget-ms>[:workers]" }).ToArray();

        public static void EnsureSupported(string strategyName)
        {
            if (IsSupported(strategyName))
            {
                return;
            }

            throw new ArgumentException(
                $"Unsupported strategy '{strategyName}'. Supported: {string.Join(", ", Supported)}.");
        }

        private static bool IsSupported(string strategyName)
        {
            if (Supported.Contains(strategyName ?? string.Empty))
            {
                return true;
            }

            return TryParseMonteCarloStrategy(strategyName, out _, out _, out _);
        }

        private static bool TryParseMonteCarloStrategy(
            string strategyName,
            out int simulations,
            out long timeBudgetMs,
            out int? workers)
        {
            simulations = 0;
            timeBudgetMs = 0;
            workers = null;

            var parts = (strategyName ?? string.Empty).Trim().Split(':');
            if ((parts.Length != 3 && parts.Length != 4) ||
                !string.Equals(parts[0], "montecarlo", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!int.TryParse(parts[1], out simulations) ||
                !long.TryParse(parts[2], out timeBudgetMs))
            {
                return false;
            }

            if (parts.Length == 4)
            {
                if (!int.TryParse(parts[3], out var parsedWorkers) || parsedWorkers <= 0)
                {
                    return false;
                }

                workers = parsedWorkers;
            }

            return simulations > 0 && timeBudgetMs > 0;
        }

        public static IPlayStrategy Create(string strategyName)
        {
            if (TryParseMonteCarloStrategy(strategyName, out var simulations, out var timeBudgetMs, out var workers))
            {
                return new MonteCarloStrategy(simulations, timeBudgetMs, workers);
            }

            switch ((strategyName ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "normal":
                    return new HeuristicPlayStrategy(
                        new FilterNoneStrategy(),
                        heuristicOptions: new HeuristicOptions());
                case "heuristic-continuations":
                    return new HeuristicPlayStrategy(
                        new FilterContinuations(5, false),
                        heuristicOptions: new HeuristicOptions());
                case "random":
                    return new RandomPlayStrategy();
                case "montecarlo-fast":
                    return new MonteCarloStrategy(20, 1);
                default:
                    EnsureSupported(strategyName);
                    throw new InvalidOperationException("Strategy validation failed unexpectedly.");
            }
        }
    }
}
