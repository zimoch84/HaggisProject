using System;
using System.Collections.Generic;
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

        public static IReadOnlyCollection<string> SupportedStrategyNames => Supported;

        public static void EnsureSupported(string strategyName)
        {
            if (!Supported.Contains(strategyName ?? string.Empty))
            {
                throw new ArgumentException(
                    $"Unsupported strategy '{strategyName}'. Supported: {string.Join(", ", Supported)}.");
            }
        }

        public static IPlayStrategy Create(string strategyName)
        {
            switch ((strategyName ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "normal":
                    return new HeuristicPlayStrategy(
                        new FilterNoneStrategy(),
                        new ContinuationTrickStrategy(false, true));
                case "heuristic-continuations":
                    return new HeuristicPlayStrategy(
                        new FilterContinuations(5, false),
                        new ContinuationTrickStrategy(false, true));
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
