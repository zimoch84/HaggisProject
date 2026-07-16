using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Haggis.AI.Benchmark;

try
{
    Trace.Listeners.Clear();

    var mode = GetMode(args);
    if (string.Equals(mode, "tuning", StringComparison.OrdinalIgnoreCase))
    {
        var options = HeuristicTuningArgumentParser.Parse(args);
        var produced = new HeuristicTuningRunner().Run(options);

        Console.WriteLine();
        Console.WriteLine($"Tuning CSV: {options.CsvPath}");
        Console.WriteLine($"Weight sets requested: {options.WeightSets.Count}");
        Console.WriteLine($"Weight sets executed: {produced.Count}");
        Console.WriteLine($"Weight sets skipped (resume): {options.WeightSets.Count - produced.Count}");

        foreach (var result in produced)
        {
            Console.WriteLine(
                $"run heuristicWinRate={result.HeuristicWinRatePct:0.00}% monteCarloWinRate={result.MonteCarloWinRatePct:0.00}% heuristicAvgScore={result.HeuristicAverageScore:0.00} monteCarloAvgScore={result.MonteCarloAverageScore:0.00} weights={HeuristicOptionsSerializer.Serialize(result.HeuristicOptions)}");
        }

        var best = produced
            .OrderByDescending(result => result.HeuristicWinRatePct)
            .ThenByDescending(result => result.HeuristicAverageScore)
            .FirstOrDefault();

        if (best != null)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Best new run: heuristicWinRate={best.HeuristicWinRatePct:0.00}% heuristicAvgScore={best.HeuristicAverageScore:0.00} weights={HeuristicOptionsSerializer.Serialize(best.HeuristicOptions)}");
        }

        Environment.ExitCode = 0;
    }
    else if (string.Equals(mode, "genetic", StringComparison.OrdinalIgnoreCase))
    {
        var options = HeuristicGeneticTuningArgumentParser.Parse(args);
        var produced = new HeuristicGeneticTuningRunner(
            new HeuristicTuningEvaluator(),
            new Random(options.RandomSeed),
            message => Console.WriteLine(message)).Run(options);

        Console.WriteLine();
        Console.WriteLine($"Genetic tuning CSV: {options.CsvPath}");
        Console.WriteLine($"Generations: {options.Generations}");
        Console.WriteLine($"Children per generation: {options.ChildrenPerGeneration}");
        Console.WriteLine($"Produced new runs: {produced.Count}");

        var best = produced
            .OrderByDescending(result => result.HeuristicWins)
            .ThenByDescending(result => result.HeuristicWinRatePct)
            .ThenByDescending(result => result.HeuristicAverageScore)
            .FirstOrDefault();

        if (best != null)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Best new run: heuristicWins={best.HeuristicWins} heuristicWinRate={best.HeuristicWinRatePct:0.00}% heuristicAvgScore={best.HeuristicAverageScore:0.00} weights={HeuristicOptionsSerializer.Serialize(best.HeuristicOptions)}");
        }

        Environment.ExitCode = 0;
    }
    else
    {
        var options = AiBenchmarkArgumentParser.Parse(args);
        var results = new AiBenchmarkRunner().Run(options);
        var summary = new AiBenchmarkSummary(results);

        Console.WriteLine(summary.Format());

        var outputTimestamp = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(options.CsvPath))
        {
            var csvPath = AiBenchmarkOutputPath.WithRunSuffix(options.CsvPath, options, outputTimestamp);
            AiBenchmarkCsvWriter.Write(csvPath, results);
            Console.WriteLine();
            Console.WriteLine($"CSV written: {csvPath}");

            var heuristicCsvPath = Path.Combine(
                Path.GetDirectoryName(csvPath) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(csvPath)}_heuristic-weights{Path.GetExtension(csvPath)}");
            AiBenchmarkHeuristicWeightsCsvWriter.Write(heuristicCsvPath, options, results);
            Console.WriteLine($"Heuristic weights CSV written: {heuristicCsvPath}");
        }

        if (!string.IsNullOrWhiteSpace(options.LogPath))
        {
            var logPath = AiBenchmarkOutputPath.WithRunSuffix(options.LogPath, options, outputTimestamp);
            AiBenchmarkGameLogWriter.Write(logPath, results);
            Console.WriteLine();
            Console.WriteLine($"Game log written: {logPath}");
        }

        Environment.ExitCode = summary.FailedGames == 0 ? 0 : 1;
    }
}
catch (Exception exception)
{
    var mode = GetMode(args);
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine("Supported arguments:");
    foreach (var argument in string.Equals(mode, "tuning", StringComparison.OrdinalIgnoreCase)
                 ? HeuristicTuningArgumentParser.SupportedArguments()
                 : string.Equals(mode, "genetic", StringComparison.OrdinalIgnoreCase)
                     ? HeuristicGeneticTuningArgumentParser.SupportedArguments()
                     : AiBenchmarkArgumentParser.SupportedArguments())
    {
        Console.Error.WriteLine($"  {argument}");
    }
    Console.Error.WriteLine();
    Console.Error.WriteLine("Supported strategies:");
    foreach (var strategy in AiBenchmarkStrategyFactory.SupportedStrategyNames)
    {
        Console.Error.WriteLine($"  {strategy}");
    }

    Environment.ExitCode = 2;
}

static string GetMode(string[] args)
{
    foreach (var argument in args ?? Array.Empty<string>())
    {
        if (string.IsNullOrWhiteSpace(argument) || !argument.StartsWith("--mode=", StringComparison.Ordinal))
        {
            continue;
        }

        return argument.Substring("--mode=".Length).Trim();
    }

    return "benchmark";
}
