import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/local_game/ai/heuristic_models.dart';
import 'package:haggis_flutter/local_game/ai/redundant_wild_assignment_filter.dart';

void main() {
  test('keeps the equivalent trick using fewer wildcards', () {
    final oneWild = _trick('one', <HeuristicCard>[
      _natural(10, 2),
      _natural(10, 3),
      _natural(10, 5),
      _wild(11, 10, 4),
    ]);
    final twoWilds = _trick('two', <HeuristicCard>[
      _natural(10, 2),
      _natural(10, 3),
      _wild(11, 10, 5),
      _wild(12, 10, 4),
    ]);

    expect(
      filterRedundantWildAssignments(<HeuristicTrick>[twoWilds, oneWild]),
      <HeuristicTrick>[oneWild],
    );
  });

  test('keeps the lower wildcard for an equivalent assignment', () {
    final jack = _trick('jack', <HeuristicCard>[
      _natural(10, 2),
      _natural(10, 3),
      _natural(10, 5),
      _wild(11, 10, 4),
    ]);
    final queen = _trick('queen', <HeuristicCard>[
      _natural(10, 2),
      _natural(10, 3),
      _natural(10, 5),
      _wild(12, 10, 4),
    ]);

    expect(
      filterRedundantWildAssignments(<HeuristicTrick>[queen, jack]),
      <HeuristicTrick>[jack],
    );
  });

  test('does not merge sequence continuations of different suits', () {
    final yellow = _sequence('yellow', 4);
    final green = _sequence('green', 3);

    expect(
      filterRedundantWildAssignments(<HeuristicTrick>[yellow, green]),
      containsAll(<HeuristicTrick>[yellow, green]),
    );
  });
}

HeuristicCard _natural(int rank, int suit) => HeuristicCard(
  rank: rank,
  baseRank: rank,
  suit: suit,
  effectiveSuit: suit,
  label: '$rank:$suit',
);

HeuristicCard _wild(int baseRank, int rank, int suit) => HeuristicCard(
  rank: rank,
  baseRank: baseRank,
  suit: 0,
  effectiveSuit: suit,
  label: '$baseRank[$rank:$suit]',
);

HeuristicTrick _trick(String description, List<HeuristicCard> cards) =>
    HeuristicTrick(
      type: 'QUAD',
      description: description,
      typeValue: 40,
      bombRank: 0,
      trickClass: 'same',
      cards: cards,
    );

HeuristicTrick _sequence(String description, int suit) => HeuristicTrick(
  type: 'SEQ3',
  description: description,
  typeValue: 32,
  bombRank: 0,
  trickClass: 'sequence',
  cards: <HeuristicCard>[
    _natural(8, suit),
    _wild(11, 9, suit),
    _natural(10, suit),
  ],
);
