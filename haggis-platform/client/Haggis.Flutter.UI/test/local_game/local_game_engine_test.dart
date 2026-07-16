import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/local_game/local_game_engine.dart';
import 'package:haggis_flutter/models/single_player_models.dart';

void main() {
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
}
