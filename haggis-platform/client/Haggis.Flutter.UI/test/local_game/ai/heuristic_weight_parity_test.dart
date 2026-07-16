import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/local_game/local_game_engine.dart';

void main() {
  test('opening ranking weights match the C# reference ranker', () {
    final ranked = rankLocalHeuristicOpening(<String>[
      '2R',
      '2B',
      '2G',
      '5G',
      '6R',
      '6O',
      '7G',
    ]);

    expect(
      ranked.map(
        (candidate) => (candidate.action.description, candidate.weight),
      ),
      <(String, int)>[
        ('SINGLE[7G]', 40),
        ('SINGLE[5G]', 38),
        ('PAIR[6R|6O]', 0),
        ('TRIPLE[2R|2B|2G]', -2),
        ('PAIR[2R|2B]', -26),
        ('PAIR[2R|2G]', -26),
        ('PAIR[2B|2G]', -26),
        ('SINGLE[6R]', -73),
        ('SINGLE[6O]', -73),
        ('SINGLE[2R]', -75),
        ('SINGLE[2B]', -75),
        ('SINGLE[2G]', -75),
      ],
    );
    expect(ranked[4].breakdown, <String, int>{
      'PreferTricksThatAreMostLikelyNonBreakableWeightStrategy': 0,
      'PreferLowerTricksWhenHandIsLargeWeightStrategy': -2,
      'PreferShorterTricksWhenHandIsLargeWeightStrategy': 0,
      'PenalizeBombOpeningWeightStrategy': 0,
      'PenalizeWildCardsInOpeningWeightStrategy': 0,
      'PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy': -73,
      'PreferTricksWithMoreContinuationsWeightStrategy': 49,
      'PreferSinglesNotBreakingNonWildCombinationsWeightStrategy': 0,
    });
  });

  test('continuation ranking weights match the C# reference ranker', () {
    final ranked = rankLocalHeuristicContinuation(<String>[
      'J',
      '8B',
      '9B',
      '10B',
    ], 'SEQ3[6O|7O|8O]');

    expect(
      ranked.map(
        (candidate) => (candidate.action.description, candidate.weight),
      ),
      <(String, int)>[
        ('SEQ3[8B|9B|10B]', 37),
        ('SEQ3[J[7B]|8B|9B]', 27),
        ('SEQ3[9B|10B|J[J]]', 0),
      ],
    );
    expect(ranked[1].breakdown, <String, int>{
      'PenalizeBombContinuationWeightStrategy': 0,
      'PenalizeWildCardsInContinuationWeightStrategy': 0,
      'PenalizeContinuationWhenHigherRelatedCombinationExistsWeightStrategy': 0,
      'PreferUsingWildAsHigherCardInContinuationWeightStrategy': -46,
      'PreferLowerValueContinuationWeightStrategy': 73,
      'PreferContinuationsWithFollowUpWeightStrategy': 0,
      'PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy': 0,
    });
  });
}
