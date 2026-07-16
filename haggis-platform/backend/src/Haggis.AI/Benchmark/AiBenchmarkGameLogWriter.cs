using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Haggis.AI.Benchmark
{
    public static class AiBenchmarkGameLogWriter
    {
        public static void Write(string path, IEnumerable<AiBenchmarkGameResult> results)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var lines = new List<string>();
            foreach (var result in results)
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                if (result.LogLines != null && result.LogLines.Count > 0)
                {
                    lines.AddRange(result.LogLines);
                    continue;
                }

                lines.Add($"GAME seed={result.Seed} rotation={result.Rotation}");
                lines.Add(result.Completed ? "  completed" : $"  failed: {result.Error}");
            }

            File.WriteAllLines(path, lines, Encoding.UTF8);
        }
    }
}
