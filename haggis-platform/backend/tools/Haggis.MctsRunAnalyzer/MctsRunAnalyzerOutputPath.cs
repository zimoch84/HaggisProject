using System;
using System.IO;
using System.Linq;

namespace Haggis.MctsRunAnalyzer
{
    public static class MctsRunAnalyzerOutputPath
    {
        public static string WithRunSuffix(string path, MctsRunAnalyzerOptions options, DateTime timestamp)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BuildDefaultPath(options, timestamp);
            }

            var directory = Path.GetDirectoryName(path);
            var extension = Path.GetExtension(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "mcts-run";
            }

            var suffix = $"{timestamp:yyyyMMdd_HHmmss}_{SanitizeFileNamePart(options?.StrategyLabel ?? "unknown")}_seed{options?.Seed ?? 0}_r{options?.RoundNumber ?? 0}_m{options?.MoveNumber ?? 0}";
            var suffixedFileName = $"{fileName}_{suffix}{extension}";

            return string.IsNullOrWhiteSpace(directory)
                ? suffixedFileName
                : Path.Combine(directory, suffixedFileName);
        }

        private static string BuildDefaultPath(MctsRunAnalyzerOptions options, DateTime timestamp)
        {
            var fileName = $"mcts-run_{timestamp:yyyyMMdd_HHmmss}_{SanitizeFileNamePart(options?.StrategyLabel ?? "unknown")}_seed{options?.Seed ?? 0}_r{options?.RoundNumber ?? 0}_m{options?.MoveNumber ?? 0}.txt";
            return Path.Combine("logs", fileName);
        }

        private static string SanitizeFileNamePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(value
                .Trim()
                .Select(character => invalidChars.Contains(character) ? '-' : character)
                .ToArray());

            return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
        }
    }
}
