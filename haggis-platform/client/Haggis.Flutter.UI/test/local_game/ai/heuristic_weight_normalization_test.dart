import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/local_game/ai/heuristic_weight_normalization.dart';

void main() {
  test('rounds midpoint away from zero like C#', () {
    expect(HeuristicWeightNormalization.baseScore(0.25, 10), 3);
    expect(HeuristicWeightNormalization.baseScore(-0.25, 10), -3);
    expect(HeuristicWeightNormalization.applyWeight(10, 2.455), 25);
  });

  test('normalizes and clamps signals', () {
    expect(HeuristicWeightNormalization.normalizeRatio(3, 2), 1);
    expect(HeuristicWeightNormalization.normalizeRatio(-1, 2), 0);
    expect(HeuristicWeightNormalization.handPhase(17), 1);
  });
}
