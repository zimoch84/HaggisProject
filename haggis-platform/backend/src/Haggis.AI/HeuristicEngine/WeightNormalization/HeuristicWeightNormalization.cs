using System;

namespace Haggis.AI.WeightNormalization
{
    internal static class HeuristicWeightNormalization
    {
        public const int MaxHandSize = 17;

        public static int BaseScore(double normalizedSignal, int baseImpact)
        {
            if (baseImpact <= 0)
            {
                return 0;
            }

            var clampedSignal = Clamp(normalizedSignal, -1d, 1d);
            return (int)Math.Round(clampedSignal * baseImpact, MidpointRounding.AwayFromZero);
        }

        public static int ApplyWeight(int baseScore, float weight)
        {
            if (weight <= 0f || baseScore == 0)
            {
                return 0;
            }

            return (int)Math.Round(baseScore * weight, MidpointRounding.AwayFromZero);
        }

        public static double Normalize(double value, double min, double max)
        {
            if (max <= min)
            {
                return 0d;
            }

            return Clamp((value - min) / (max - min), 0d, 1d);
        }

        public static double NormalizeRatio(double numerator, double denominator)
        {
            if (denominator <= 0d)
            {
                return 0d;
            }

            return Clamp(numerator / denominator, 0d, 1d);
        }

        public static double HandPhase(int handCount)
        {
            return NormalizeRatio(handCount, MaxHandSize);
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
