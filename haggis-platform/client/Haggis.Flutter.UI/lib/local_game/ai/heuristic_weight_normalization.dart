import 'dart:math' as math;

/// Exact counterpart of HeuristicWeightNormalization in Haggis.AI.
abstract final class HeuristicWeightNormalization {
  static const int maxHandSize = 17;

  static int baseScore(double normalizedSignal, int baseImpact) {
    if (baseImpact <= 0) return 0;
    return _roundAwayFromZero(clamp(normalizedSignal, -1, 1) * baseImpact);
  }

  static int applyWeight(int baseScore, double weight) {
    if (weight <= 0 || baseScore == 0) return 0;
    return _roundAwayFromZero(baseScore * weight);
  }

  static double normalize(double value, double min, double max) {
    if (max <= min) return 0;
    return clamp((value - min) / (max - min), 0, 1);
  }

  static double normalizeRatio(num numerator, num denominator) {
    if (denominator <= 0) return 0;
    return clamp(numerator / denominator, 0, 1);
  }

  static double handPhase(int handCount) =>
      normalizeRatio(handCount, maxHandSize);

  static double clamp(double value, double min, double max) =>
      math.max(min, math.min(max, value));

  static int _roundAwayFromZero(double value) =>
      value >= 0 ? (value + 0.5).floor() : (value - 0.5).ceil();
}
