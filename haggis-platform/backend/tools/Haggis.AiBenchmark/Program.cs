using System;
using Haggis.AI.Benchmark;

try
{
    var options = AiBenchmarkArgumentParser.Parse(args);
    var results = new AiBenchmarkRunner().Run(options);
    var summary = new AiBenchmarkSummary(results);

    Console.WriteLine(summary.Format());

    if (!string.IsNullOrWhiteSpace(options.CsvPath))
    {
        AiBenchmarkCsvWriter.Write(options.CsvPath, results);
        Console.WriteLine();
        Console.WriteLine($"CSV written: {options.CsvPath}");
    }

    if (!string.IsNullOrWhiteSpace(options.LogPath))
    {
        AiBenchmarkGameLogWriter.Write(options.LogPath, results);
        Console.WriteLine();
        Console.WriteLine($"Game log written: {options.LogPath}");
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
