using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Haggis.AI.Strategies;

namespace Haggis.AI.Benchmark
{
    public static class HeuristicOptionsSerializer
    {
        private static readonly PropertyInfo[] WeightProperties = typeof(HeuristicOptions)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(float))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        public static IReadOnlyList<string> WeightPropertyNames { get; } =
            WeightProperties.Select(property => property.Name).ToArray();

        public static HeuristicOptions Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var options = new HeuristicOptions();
            var segments = text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawSegment in segments)
            {
                var segment = rawSegment.Trim();
                var separatorIndex = segment.IndexOf('=');
                if (separatorIndex <= 0 || separatorIndex == segment.Length - 1)
                {
                    throw new ArgumentException(
                        $"Invalid heuristic weight segment '{segment}'. Expected name=value.");
                }

                var name = segment.Substring(0, separatorIndex).Trim();
                var valueText = segment.Substring(separatorIndex + 1).Trim();
                var property = WeightProperties.FirstOrDefault(item =>
                    string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                if (property == null)
                {
                    throw new ArgumentException($"Unknown heuristic weight '{name}'.");
                }

                if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    throw new ArgumentException(
                        $"Invalid value '{valueText}' for heuristic weight '{name}'.");
                }

                property.SetValue(options, value);
            }

            return options;
        }

        public static string Serialize(HeuristicOptions options)
        {
            if (options == null)
            {
                return string.Empty;
            }

            return string.Join(
                ";",
                WeightProperties.Select(property =>
                    $"{property.Name}={((float)property.GetValue(options)).ToString("0.###", CultureInfo.InvariantCulture)}"));
        }

        public static IReadOnlyDictionary<string, float> ToDictionary(HeuristicOptions options)
        {
            return WeightProperties.ToDictionary(
                property => property.Name,
                property => options == null ? 0f : (float)property.GetValue(options),
                StringComparer.Ordinal);
        }

        public static HeuristicOptions Clone(HeuristicOptions options)
        {
            if (options == null)
            {
                return null;
            }

            return Parse(Serialize(options));
        }

        public static float GetWeight(HeuristicOptions options, string propertyName)
        {
            var property = WeightProperties.FirstOrDefault(item =>
                string.Equals(item.Name, propertyName, StringComparison.Ordinal));
            if (property == null)
            {
                throw new ArgumentException($"Unknown heuristic weight '{propertyName}'.", nameof(propertyName));
            }

            return options == null ? 0f : (float)property.GetValue(options);
        }

        public static void SetWeight(HeuristicOptions options, string propertyName, float value)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var property = WeightProperties.FirstOrDefault(item =>
                string.Equals(item.Name, propertyName, StringComparison.Ordinal));
            if (property == null)
            {
                throw new ArgumentException($"Unknown heuristic weight '{propertyName}'.", nameof(propertyName));
            }

            property.SetValue(options, value);
        }
    }
}
