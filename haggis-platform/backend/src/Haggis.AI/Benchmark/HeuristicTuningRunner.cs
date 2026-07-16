using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Strategies;

namespace Haggis.AI.Benchmark
{
    public sealed class HeuristicTuningRunner
    {
        private readonly IHeuristicTuningEvaluator _evaluator;

        public HeuristicTuningRunner()
            : this(new HeuristicTuningEvaluator())
        {
        }

        public HeuristicTuningRunner(IHeuristicTuningEvaluator evaluator)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public IReadOnlyList<HeuristicTuningRunResult> Run(HeuristicTuningBatchOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return options.WeightSets != null && options.WeightSets.Count > 0
                ? RunExplicitWeightSets(options)
                : RunOneAtATime(options);
        }

        private IReadOnlyList<HeuristicTuningRunResult> RunExplicitWeightSets(HeuristicTuningBatchOptions options)
        {
            var results = new List<HeuristicTuningRunResult>();
            var existingResults = HeuristicTuningCsvWriter.LoadExistingResults(options.CsvPath)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

            foreach (var weightSet in options.WeightSets ?? Array.Empty<HeuristicOptions>())
            {
                var probe = new HeuristicTuningRunResult
                {
                    Games = options.Games,
                    SeedStart = options.SeedStart,
                    Rotate = options.Rotate,
                    GameOverScore = options.GameOverScore,
                    MaxMovesPerGame = options.MaxMovesPerGame,
                    MonteCarloStrategy = options.MonteCarloStrategy,
                    HeuristicOptions = HeuristicOptionsSerializer.Clone(weightSet)
                };
                var key = HeuristicTuningCsvWriter.BuildKey(probe);
                if (existingResults.ContainsKey(key))
                {
                    continue;
                }

                var result = GetOrEvaluate(options, weightSet, existingResults);
                results.Add(result);
            }

            return results;
        }

        private IReadOnlyList<HeuristicTuningRunResult> RunOneAtATime(HeuristicTuningBatchOptions options)
        {
            var results = new List<HeuristicTuningRunResult>();
            var existingResults = HeuristicTuningCsvWriter.LoadExistingResults(options.CsvPath)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            var baselineWeights = CreateBaselineOptions(options.BaselineWeight);
            var baselineResult = GetOrEvaluate(options, baselineWeights, existingResults);
            results.Add(baselineResult);

            foreach (var weightName in HeuristicOptionsSerializer.WeightPropertyNames)
            {
                var previousResult = baselineResult;
                var currentWeight = options.BaselineWeight + options.WeightStep;

                while (true)
                {
                    var candidateWeights = CreateBaselineOptions(options.BaselineWeight);
                    HeuristicOptionsSerializer.SetWeight(candidateWeights, weightName, currentWeight);
                    var candidateResult = GetOrEvaluate(options, candidateWeights, existingResults);
                    results.Add(candidateResult);

                    if (!IsImprovement(candidateResult, previousResult))
                    {
                        break;
                    }

                    previousResult = candidateResult;
                    currentWeight = (float)Math.Round(currentWeight + options.WeightStep, 3, MidpointRounding.AwayFromZero);
                }

                currentWeight = (float)Math.Round(options.BaselineWeight - options.WeightStep, 3, MidpointRounding.AwayFromZero);
                while (currentWeight >= 0f)
                {
                    var candidateWeights = CreateBaselineOptions(options.BaselineWeight);
                    HeuristicOptionsSerializer.SetWeight(candidateWeights, weightName, currentWeight);
                    var candidateResult = GetOrEvaluate(options, candidateWeights, existingResults);
                    results.Add(candidateResult);
                    currentWeight = (float)Math.Round(currentWeight - options.WeightStep, 3, MidpointRounding.AwayFromZero);
                }
            }

            return results;
        }

        private HeuristicTuningRunResult GetOrEvaluate(
            HeuristicTuningBatchOptions options,
            HeuristicOptions weightSet,
            IDictionary<string, HeuristicTuningRunResult> existingResults)
        {
            var probe = new HeuristicTuningRunResult
            {
                Games = options.Games,
                SeedStart = options.SeedStart,
                Rotate = options.Rotate,
                GameOverScore = options.GameOverScore,
                MaxMovesPerGame = options.MaxMovesPerGame,
                MonteCarloStrategy = options.MonteCarloStrategy,
                HeuristicOptions = HeuristicOptionsSerializer.Clone(weightSet)
            };
            var key = HeuristicTuningCsvWriter.BuildKey(probe);
            if (existingResults.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var result = _evaluator.Evaluate(options, weightSet);
            HeuristicTuningCsvWriter.Append(options.CsvPath, result);
            existingResults[HeuristicTuningCsvWriter.BuildKey(result)] = result;
            return result;
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

        private static bool IsImprovement(HeuristicTuningRunResult candidate, HeuristicTuningRunResult previous)
        {
            if (candidate.HeuristicWinRatePct > previous.HeuristicWinRatePct)
            {
                return true;
            }

            return Math.Abs(candidate.HeuristicWinRatePct - previous.HeuristicWinRatePct) < 0.0001 &&
                   candidate.HeuristicAverageScore > previous.HeuristicAverageScore;
        }
    }
}
