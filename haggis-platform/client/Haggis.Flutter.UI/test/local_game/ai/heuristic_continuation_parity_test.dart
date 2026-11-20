import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/local_game/local_game_engine.dart';

void main() {
  final cases = <(List<String>, String, String)>[
    (<String>['J', '8B', '9B', '10B'], 'SEQ3[6O|7O|8O]', 'SEQ3[8B|9B|10B]'),
    (
      <String>['2R', '2B', '3R', '3B', '4R', '4B'],
      'PAIR[2G|2O]',
      'PAIR[3R|3B]',
    ),
    (
      <String>['3R', '3B', '4R', '4B', '5R', '5B'],
      'PAIR[2G|2O]',
      'PAIR[3R|3B]',
    ),
  ];

  for (final fixture in cases) {
    test('matches C# continuation after ${fixture.$2}', () {
      expect(
        chooseLocalHeuristicContinuation(fixture.$1, fixture.$2),
        fixture.$3,
      );
    });
  }
}
