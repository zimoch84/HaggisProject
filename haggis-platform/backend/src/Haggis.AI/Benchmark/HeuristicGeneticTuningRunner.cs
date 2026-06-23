using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Strategies;

namespace Haggis.AI.Benchmark
{
    public sealed class HeuristicGeneticTuningRunner
    {
        private readonly IHeuristicTuningEvaluator _evaluator;
        private readonly Random _random;
        private readonly Action<string> _log;

        public HeuristicGeneticTuningRunner()
            : this(new HeuristicTuningEvaluator(), new Random(12345), null)
        {
        }

        public HeuristicGeneticTuningRunner(IHeuristicTuningEvaluator evaluator, Random random, Action<string> log)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _log = log;
        }

        public IReadOnlyList<HeuristicTuningRunResult> Run(HeuristicGeneticTuningOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var produced = new List<HeuristicTuningRunResult>();
            var existingResults = HeuristicTuningCsvWriter.LoadExistingResults(options.CsvPath)
                .Where(item => MatchesConfig(item.Value, options))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

            if (existingResults.Count == 0)
            {
                var baseline = CreateBaselineOptions(options.BaselineWeight);
                var baselineResult = GetOrEvaluate(options, baseline, existingResults, out var baselineCreated, out _);
                if (baselineCreated)
                {
                    produced.Add(baselineResult);
                }
            }

            for (var generation = 0; generation < options.Generations; generation++)
            {
                var parents = SelectTopParents(existingResults.Values, options.TopParentCount);
                if (parents.Count == 0)
                {
                    break;
                }

                _log?.Invoke(
                    $"genetic generation={generation + 1} selectedParents={parents.Count}");
                for (var parentIndex = 0; parentIndex < parents.Count; parentIndex++)
                {
                    var parent = parents[parentIndex];
                    _log?.Invoke(
                        $"genetic generation={generation + 1} parent[{parentIndex + 1}] heuristicWins={parent.HeuristicWins} heuristicWinRate={parent.HeuristicWinRatePct:0.00}% heuristicAvgScore={parent.HeuristicAverageScore:0.00} weights={HeuristicOptionsSerializer.Serialize(parent.HeuristicOptions)}");
                }

                var createdThisGeneration = 0;
                var maxAttempts = Math.Max(options.ChildrenPerGeneration * 10, 50);
                for (var attempt = 0; attempt < maxAttempts && createdThisGeneration < options.ChildrenPerGeneration; attempt++)
                {
                    var parentA = parents[_random.Next(parents.Count)];
                    var parentB = parents[_random.Next(parents.Count)];
                    var child = Crossover(parentA.HeuristicOptions, parentB.HeuristicOptions);
                    var mutationDescription = Mutate(child, options);

                    _log?.Invoke(
                        $"genetic generation={generation + 1} childAttempt={attempt + 1} parentA[wins={parentA.HeuristicWins},winRate={parentA.HeuristicWinRatePct:0.00}%,avgScore={parentA.HeuristicAverageScore:0.00}] parentB[wins={parentB.HeuristicWins},winRate={parentB.HeuristicWinRatePct:0.00}%,avgScore={parentB.HeuristicAverageScore:0.00}] mutate={mutationDescription}");

                    var result = GetOrEvaluate(options, child, existingResults, out var created, out var skippedFromCache);
                    if (!created)
                    {
                        if (skippedFromCache)
                        {
                            _log?.Invoke(
                                $"genetic generation={generation + 1} childAttempt={attempt + 1} skipped=resume heuristicWins={result.HeuristicWins} heuristicWinRate={result.HeuristicWinRatePct:0.00}% heuristicAvgScore={result.HeuristicAverageScore:0.00} weights={HeuristicOptionsSerializer.Serialize(result.HeuristicOptions)}");
                        }
                        continue;
                    }

                    _log?.Invoke(
                        $"genetic generation={generation + 1} childAccepted={createdThisGeneration + 1}/{options.ChildrenPerGeneration} heuristicWins={result.HeuristicWins} heuristicWinRate={result.HeuristicWinRatePct:0.00}% heuristicAvgScore={result.HeuristicAverageScore:0.00} weights={HeuristicOptionsSerializer.Serialize(result.HeuristicOptions)}");
                    produced.Add(result);
                    createdThisGeneration++;
                }
            }

            return produced;
        }

        private HeuristicTuningRunResult GetOrEvaluate(
            HeuristicGeneticTuningOptions options,
            HeuristicOptions weightSet,
            IDictionary<string, HeuristicTuningRunResult> existingResults,
            out bool created,
            out bool skippedFromCache)
        {
            var probe = CreateProbe(options, weightSet);
            var key = HeuristicTuningCsvWriter.BuildKey(probe);
            if (existingResults.TryGetValue(key, out var existing))
            {
                created = false;
                skippedFromCache = true;
                return existing;
            }

            var result = _evaluator.Evaluate(ToBatchOptions(options), weightSet);
            HeuristicTuningCsvWriter.Append(options.CsvPath, result);
            existingResults[HeuristicTuningCsvWriter.BuildKey(result)] = result;
            created = true;
            skippedFromCache = false;
            return result;
        }

        private static HeuristicTuningRunResult CreateProbe(HeuristicGeneticTuningOptions options, HeuristicOptions weightSet)
        {
            return new HeuristicTuningRunResult
            {
                Games = options.Games,
                SeedStart = options.SeedStart,
                Rotate = options.Rotate,
                GameOverScore = options.GameOverScore,
                MaxMovesPerGame = options.MaxMovesPerGame,
                MonteCarloStrategy = options.MonteCarloStrategy,
                HeuristicOptions = HeuristicOptionsSerializer.Clone(weightSet)
            };
        }

        private static HeuristicTuningBatchOptions ToBatchOptions(HeuristicGeneticTuningOptions options)
        {
            return new HeuristicTuningBatchOptions
            {
                Games = options.Games,
                SeedStart = options.SeedStart,
                Rotate = options.Rotate,
                GameOverScore = options.GameOverScore,
                MaxMovesPerGame = options.MaxMovesPerGame,
                MonteCarloStrategy = options.MonteCarloStrategy,
                CsvPath = options.CsvPath,
                BaselineWeight = options.BaselineWeight
            };
        }

        private static List<HeuristicTuningRunResult> SelectTopParents(
            IEnumerable<HeuristicTuningRunResult> results,
            int topParentCount)
        {
            return results
                .OrderByDescending(result => result.HeuristicWins)
                .ThenByDescending(result => result.HeuristicWinRatePct)
                .ThenByDescending(result => result.HeuristicAverageScore)
                .Take(topParentCount)
                .ToList();
        }

        private HeuristicOptions Crossover(HeuristicOptions parentA, HeuristicOptions parentB)
        {
            var child = new HeuristicOptions();
            foreach (var weightName in HeuristicOptionsSerializer.WeightPropertyNames)
            {
                var value = _random.NextDouble() < 0.5
                    ? HeuristicOptionsSerializer.GetWeight(parentA, weightName)
                    : HeuristicOptionsSerializer.GetWeight(parentB, weightName);
                HeuristicOptionsSerializer.SetWeight(child, weightName, value);
            }

            return child;
        }

        private string Mutate(HeuristicOptions child, HeuristicGeneticTuningOptions options)
        {
            var mutations = new List<string>();
            var mutatedAny = false;
            foreach (var weightName in HeuristicOptionsSerializer.WeightPropertyNames)
            {
                if (_random.NextDouble() > options.MutationRate)
                {
                    continue;
                }

                var current = HeuristicOptionsSerializer.GetWeight(child, weightName);
                var delta = NextFloat(options.MutationDeltaMin, options.MutationDeltaMax);
                var mutated = Math.Max(0f, current + delta);
                HeuristicOptionsSerializer.SetWeight(child, weightName, mutated);
                mutations.Add($"{weightName}:{current:0.###}{FormatDelta(delta)}=>{mutated:0.###}");
                mutatedAny = true;
            }

            if (mutatedAny)
            {
                return string.Join(", ", mutations);
            }

            var forcedWeight = HeuristicOptionsSerializer.WeightPropertyNames[_random.Next(HeuristicOptionsSerializer.WeightPropertyNames.Count)];
            var currentValue = HeuristicOptionsSerializer.GetWeight(child, forcedWeight);
            var forcedDelta = NextFloat(options.MutationDeltaMin, options.MutationDeltaMax);
            var forcedMutatedValue = Math.Max(0f, currentValue + forcedDelta);
            HeuristicOptionsSerializer.SetWeight(child, forcedWeight, forcedMutatedValue);
            return $"{forcedWeight}:{currentValue:0.###}{FormatDelta(forcedDelta)}=>{forcedMutatedValue:0.###} (forced)";
        }

        private float NextFloat(float min, float max)
        {
            if (Math.Abs(max - min) < 0.0001f)
            {
                return min;
            }

            return (float)(min + _random.NextDouble() * (max - min));
        }

        private static HeuristicOptions CreateBaselineOptions(float baselineWeight)
        {
            var options = new HeuristicOptions();
            foreach (var weightName in HeuristicOptionsSerializer.WeightPropertyNames)
            {
                HeuristicOptionsSerializer.SetWeight(options, weightName, baselineWeight);
            }

            return options;
        }

        private static bool MatchesConfig(HeuristicTuningRunResult result, HeuristicGeneticTuningOptions options)
        {
            return result != null &&
                   result.Games == options.Games &&
                   result.SeedStart == options.SeedStart &&
                   result.Rotate == options.Rotate &&
                   result.GameOverScore == options.GameOverScore &&
                   result.MaxMovesPerGame == options.MaxMovesPerGame &&
                   string.Equals(result.MonteCarloStrategy, options.MonteCarloStrategy, StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatDelta(float delta)
        {
            return delta >= 0f
                ? $"+{delta:0.###}"
                : delta.ToString("0.###");
        }
    }
}
