import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/local_game/local_game_engine.dart';

void main() {
  final cases = <(List<String>, String)>[
    (<String>['2R', '2B', '2G', '5G', '6R', '6O', '7G'], 'SINGLE[7G]'),
    (<String>['2R', '2B', '10R'], 'SINGLE[10R]'),
    (<String>['2R', '2B', '8G', '10R'], 'SINGLE[10R]'),
    (<String>['2R', '2B', '5G', '6R', '6O'], 'PAIR[2R|2B]'),
    (<String>['7G'], 'SINGLE[7G]'),
    (<String>['2B', '2G', '3B', '4B', '6B', '6O', '7G', '10B'], 'PAIR[2B|2G]'),
  ];

  for (final fixture in cases) {
    test('matches C# opening for ${fixture.$1.join(' ')}', () {
      expect(chooseLocalHeuristicOpening(fixture.$1), fixture.$2);
    });
  }
}
