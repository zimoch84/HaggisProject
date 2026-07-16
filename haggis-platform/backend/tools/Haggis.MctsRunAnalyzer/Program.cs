using System;
using System.IO;
using Haggis.MctsRunAnalyzer;

try
{
    var options = MctsRunAnalyzerArgumentParser.Parse(args);
    var run = MctsRunAnalyzerRunner.Run(options);

    var outputTimestamp = DateTime.Now;
    var outputPath = MctsRunAnalyzerOutputPath.WithRunSuffix(options.OutputPath, options, outputTimestamp);
    MctsTraceFileWriter.Write(
        outputPath,
        options,
        run.Context,
        run.TargetPlayer,
        run.TargetStrategy,
        run.ChosenAction,
        run.SetupLines,
        run.HeuristicRanking,
        run.TraceEvents,
        run.ComputedResult);

    Console.WriteLine($"Trace written: {outputPath}");
    Console.WriteLine($"Trace link: {Path.GetFullPath(outputPath)}");
    Console.WriteLine($"Target decision: {run.TargetPlayer} [{run.TargetStrategy}] -> {run.ChosenAction}");
    Console.WriteLine(
        $"Rollouts: scheduled={run.ComputedResult?.ScheduledRollouts ?? 0} completed={run.ComputedResult?.CompletedRollouts ?? 0} iterations={run.ComputedResult?.Iterations ?? 0} workers={run.ComputedResult?.Workers ?? 0}");
    Environment.ExitCode = 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine("Supported arguments:");
    foreach (var argument in MctsRunAnalyzerArgumentParser.SupportedArguments())
    {
        Console.Error.WriteLine($"  {argument}");
    }

    Console.Error.WriteLine();
    Console.Error.WriteLine("Supported strategies:");
    foreach (var strategy in MctsRunAnalyzerArgumentParser.SupportedStrategies)
    {
        Console.Error.WriteLine($"  {strategy}");
    }

    Environment.ExitCode = 2;
}
