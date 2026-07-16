using System;
using System.IO;
using System.Linq;

namespace Haggis.AI.Benchmark
{
    public static class AiBenchmarkOutputPath
    {
        public static string WithRunSuffix(string path, AiBenchmarkOptions options, DateTime timestamp)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            var directory = Path.GetDirectoryName(path);
            var extension = Path.GetExtension(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "benchmark";
            }

            var strategyLabel = SanitizeFileNamePart(options?.StrategyLabel ?? "unknown");
            var suffix = strategyLabel;
            var suffixedFileName = $"{fileName}_{suffix}{extension}";

            return string.IsNullOrWhiteSpace(directory)
                ? suffixedFileName
                : Path.Combine(directory, suffixedFileName);
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
