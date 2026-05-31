using System;
using Haggis.AI.Benchmark;

try
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
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine("Supported arguments:");
    foreach (var argument in AiBenchmarkArgumentParser.SupportedArguments())
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
