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
            "random",
            "montecarlo-fast"
        };

        public static IReadOnlyCollection<string> SupportedStrategyNames =>
            Supported.Concat(new[]
            {
                "montecarlo:<simulations>:<budget-ms>[:workers]",
                "montecarlo:<simulations>:<budget-ms>[:workers]:<treeTopN>:<rolloutTopN>"
            }).ToArray();

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

            return TryParseMonteCarloStrategy(strategyName, out _, out _, out _, out _);
        }

        private static bool TryParseMonteCarloStrategy(
            string strategyName,
            out int simulations,
            out long timeBudgetMs,
            out int? workers,
            out MonteCarloHeuristicOptions heuristicOptions)
        {
            simulations = 0;
            timeBudgetMs = 0;
            workers = null;
            heuristicOptions = null;

            var parts = (strategyName ?? string.Empty).Trim().Split(':');
            if (parts.Length < 3 || parts.Length > 6 ||
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
                    if (!TryParseHeuristicOptions(parts, 3, out heuristicOptions))
                    {
                        return false;
                    }
                }
                else
                {
                    workers = parsedWorkers;
                    if (!TryParseHeuristicOptions(parts, 4, out heuristicOptions))
                    {
                        return false;
                    }
                }
            }
            else if (parts.Length > 4)
            {
                if (!int.TryParse(parts[3], out var parsedWorkers) || parsedWorkers <= 0)
                {
                    return false;
                }

                workers = parsedWorkers;
                if (!TryParseHeuristicOptions(parts, 4, out heuristicOptions))
                {
                    return false;
                }
            }

            return simulations > 0 && timeBudgetMs > 0;
        }

        public static IPlayStrategy Create(string strategyName, HeuristicOptions heuristicOptions = null)
        {
            if (TryParseMonteCarloStrategy(strategyName, out var simulations, out var timeBudgetMs, out var workers, out var monteCarloHeuristicOptions))
            {
                var effectiveMonteCarloHeuristicOptions = monteCarloHeuristicOptions;
                if (effectiveMonteCarloHeuristicOptions?.Enabled == true)
                {
                    effectiveMonteCarloHeuristicOptions.HeuristicOptions = heuristicOptions ?? new HeuristicOptions();
                }

                return new MonteCarloStrategy(simulations, timeBudgetMs, workers, null, effectiveMonteCarloHeuristicOptions);
            }

            switch ((strategyName ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "normal":
                    return HeuristicPlayStrategy.Create(
                        new FilterNoneStrategy(),
                        heuristicOptions: heuristicOptions ?? new HeuristicOptions());
                case "random":
                    return new RandomPlayStrategy();
                case "montecarlo-fast":
                    return new MonteCarloStrategy(20, 1);
                default:
                    EnsureSupported(strategyName);
                    throw new InvalidOperationException("Strategy validation failed unexpectedly.");
            }
        }

        private static bool TryParseHeuristicOptions(string[] parts, int startIndex, out MonteCarloHeuristicOptions heuristicOptions)
        {
            heuristicOptions = null;
            if (parts.Length <= startIndex)
            {
                return true;
            }

            if (parts.Length - startIndex != 2)
            {
                return false;
            }

            if (!int.TryParse(parts[startIndex], out var treeTopN) || treeTopN < 0)
            {
                return false;
            }

            if (!int.TryParse(parts[startIndex + 1], out var rolloutTopN) || rolloutTopN < 0)
            {
                return false;
            }

            heuristicOptions = new MonteCarloHeuristicOptions
            {
                Enabled = treeTopN > 0 || rolloutTopN > 0,
                TreeTopN = treeTopN,
                RolloutTopN = rolloutTopN
            };
            return true;
        }
    }
}
