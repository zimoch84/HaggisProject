import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/local_game/local_game_engine.dart';
import 'package:haggis_flutter/models/game_models.dart';
import 'package:haggis_flutter/models/single_player_models.dart';

void main() {
  test('does not generate a paired sequence with changing suits', () {
    final actions = localGeneratedTrickDescriptions(<String>[
      '4R',
      '4B',
      '5B',
      '5G',
    ]);

    expect(actions.where((action) => action.startsWith('PAIRSEQ')), isEmpty);
  });

  test('generates a paired sequence when suits stay consistent', () {
    final actions = localGeneratedTrickDescriptions(<String>[
      '4R',
      '4B',
      '5R',
      '5B',
    ]);

    expect(actions, contains('PAIRSEQ2[4R|4B|5R|5B]'));
  });

  test('DotNetRandom matches the seeded System.Random sequence', () {
    final random = DotNetRandom(12345);
    expect(List<int>.generate(5, (_) => random.next(1000)), <int>[
      66,
      70,
      774,
      511,
      797,
    ]);
  });

  test('local session starts and advances without a server', () async {
    final engine = LocalGameEngine(
      humanId: 'Player',
      aiPlayers: const <SinglePlayerAiConfig>[
        SinglePlayerAiConfig(name: 'AI-1', difficulty: AiDifficulty.easy),
        SinglePlayerAiConfig(name: 'AI-2', difficulty: AiDifficulty.normal),
      ],
    );

    final initial = engine.start();
    expect(initial.version, 1);
    expect(initial.roundNumber, 1);
    expect(initial.currentPlayerId, 'Player');
    expect(initial.players, hasLength(3));
    expect(initial.players.every((player) => player.handCount == 17), isTrue);
    expect(initial.possibleActions, isNotEmpty);

    final next = await engine.play(initial.possibleActions.first.displayAction);
    expect(next.version, 2);
    expect(next.appliedMoves, isNotEmpty);
    expect(next.currentPlayerId, 'Player');
  });

  test('local engine completes a round using only legal actions', () async {
    final engine = LocalGameEngine(
      humanId: 'Player',
      aiPlayers: const <SinglePlayerAiConfig>[
        SinglePlayerAiConfig(name: 'AI-1', difficulty: AiDifficulty.easy),
      ],
    );
    var state = engine.start();
    var moves = 0;
    while (state.roundNumber == 1 && !state.gameOver && moves++ < 250) {
      expect(state.currentPlayerId, 'Player');
      expect(state.possibleActions, isNotEmpty);
      final play = state.possibleActions.firstWhere(
        (action) => action.type.toLowerCase() != 'pass',
        orElse: () => state.possibleActions.first,
      );
      state = await engine.play(play.displayAction);
    }
    expect(moves, lessThan(250));
    expect(state.previousRound, isNotNull);
    expect(state.roundNumber > 1 || state.gameOver, isTrue);
  });

  test(
    'bomb player leads the next trick after earlier play and passes',
    () async {
      final moves = <TrickMove>[
        TrickMove(playerId: 'AI-1', description: 'SINGLE[10R]'),
        TrickMove(playerId: 'Player', description: 'BOMB[3R|5R|7R|9R]'),
        TrickMove(playerId: 'AI-2', description: 'Pass'),
        TrickMove(playerId: 'AI-1', description: 'Pass'),
      ];

      expect(localTrickWinnerId(moves), 'Player');
    },
  );

  test('local engine gives the lead to bomb player after AI passes', () async {
    var ai1Calls = 0;
    final engine = LocalGameEngine(
      humanId: 'Player',
      aiPlayers: const <SinglePlayerAiConfig>[
        SinglePlayerAiConfig(name: 'AI-1', difficulty: AiDifficulty.easy),
        SinglePlayerAiConfig(name: 'AI-2', difficulty: AiDifficulty.easy),
      ],
      aiActionSelector: (playerId, actions) {
        if (playerId == 'AI-1' && ai1Calls++ == 0) {
          return actions
              .firstWhere((action) => action.type != 'Pass')
              .displayAction;
        }
        return 'Pass';
      },
    );

    var state = engine.start();
    final opening = state.possibleActions.firstWhere(
      (action) => action.type != 'Pass',
    );
    state = await engine.play(opening.displayAction);

    final bomb = state.possibleActions.firstWhere(
      (action) => action.displayAction.startsWith('BOMB['),
    );
    final afterBomb = await engine.play(bomb.displayAction);

    expect(
      afterBomb.appliedMoves
          .map((move) => move.playerId)
          .where((playerId) => playerId == 'AI-1' || playerId == 'AI-2'),
      containsAll(<String>['AI-1', 'AI-2']),
    );
    expect(afterBomb.currentPlayerId, 'Player');
  });
}
