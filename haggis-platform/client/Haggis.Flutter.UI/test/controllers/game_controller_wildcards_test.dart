import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/controllers/game_controller.dart';
import 'package:haggis_flutter/models/game_models.dart';
import 'package:haggis_flutter/models/lobby_models.dart';
import 'package:haggis_flutter/models/single_player_models.dart';

void main() {
  group('GameController wildcard matching', () {
    test('matches sequence with queen wildcard assignment', () {
      final controller = _createControllerWithSnapshot(
        hand: <String>['7R', 'Q', '9R', '10R'],
        possibleActions: <String>['SEQ4[7R|Q[8]|9R|10R]'],
      );

      final matches = controller.findMatchingPlayableActions(<String>[
        '7R',
        'Q',
        '9R',
        '10R',
      ]);

      expect(matches.map((match) => match.displayAction), <String>[
        'SEQ4[7R|Q[8]|9R|10R]',
      ]);
    });

    test('offers sequence replacement option for queen wildcard', () {
      final controller = _createControllerWithSnapshot(
        hand: <String>['7R', 'Q', '9R', '10R'],
        possibleActions: <String>['SEQ4[7R|Q[8]|9R|10R]'],
      );

      final options = controller.getWildReplacementOptionsForCards(
        'Q',
        <String>['7R', 'Q', '9R', '10R'],
      );

      expect(options, <String>['8']);
    });

    test(
      'treats jack and queen symmetrically for sequence wildcard options',
      () {
        final queenController = _createControllerWithSnapshot(
          hand: <String>['7R', 'Q', '9R', '10R'],
          possibleActions: <String>['SEQ4[7R|Q[8]|9R|10R]'],
        );
        final jackController = _createControllerWithSnapshot(
          hand: <String>['7R', 'J', '9R', '10R'],
          possibleActions: <String>['SEQ4[7R|J[8]|9R|10R]'],
        );

        final queenOptions = queenController.getWildReplacementOptionsForCards(
          'Q',
          <String>['7R', 'Q', '9R', '10R'],
        );
        final jackOptions = jackController.getWildReplacementOptionsForCards(
          'J',
          <String>['7R', 'J', '9R', '10R'],
        );

        expect(queenOptions, jackOptions);
      },
    );

    test(
      'keeps other wild assignments while resolving queen wildcard options',
      () {
        final controller = _createControllerWithSnapshot(
          hand: <String>['9R', '9B', '10R', '10B', 'J', 'Q'],
          possibleActions: <String>[
            'PAIRSEQ2[9R|9B|J[10]|10B]',
            'PAIRSEQ2[9R|9B|Q[10]|10B]',
            'PAIRSEQ2[9R|9B|J[9]|Q[10]]',
          ],
        );

        final options = controller.getWildReplacementOptionsForCards(
          'Q',
          <String>['9R', '9B', 'J', 'Q'],
          wildAssignments: <String, String>{'J': '9'},
        );

        expect(options, <String>['10']);
      },
    );
  });

  test('publishes round summary when game ends without next round', () async {
    final controller = GameController(
      serverBaseUrl: 'http://localhost:6666',
      playerId: 'p1',
      room: LobbyRoom(
        roomId: 'room-1',
        gameId: 'game-1',
        roomName: 'Test Room',
        players: <String>['p1', 'AI-1', 'AI-2'],
      ),
      singlePlayer: true,
      singlePlayerAiPlayers: const <SinglePlayerAiConfig>[
        SinglePlayerAiConfig(name: 'AI-1', difficulty: AiDifficulty.hard),
        SinglePlayerAiConfig(name: 'AI-2', difficulty: AiDifficulty.expert),
      ],
    );

    controller.applyStateMessageForTesting(
      _stateMessage(
        version: 22,
        roundNumber: 1,
        currentPlayerId: 'p1',
        roundOver: false,
        gameOver: false,
        players: <Map<String, Object?>>[
          _player(id: 'p1', score: 220, handCount: 1, hand: <String>['3B']),
          _player(id: 'AI-1', score: 210, handCount: 1, hand: <String>['7O']),
          _player(id: 'AI-2', score: 240, handCount: 1, hand: <String>['5G']),
        ],
        trick: <Map<String, Object?>>[
          _move(playerId: 'AI-2', desc: 'SINGLE[5G]'),
        ],
        possibleActions: <Map<String, Object?>>[
          _possibleAction(type: 'Play', action: 'SINGLE[10Y]'),
        ],
        appliedMoves: <Map<String, Object?>>[],
      ),
    );

    controller.applyStateMessageForTesting(
      _stateMessage(
        version: 23,
        roundNumber: 1,
        currentPlayerId: 'p1',
        roundOver: true,
        gameOver: true,
        players: <Map<String, Object?>>[
          _player(
            id: 'p1',
            score: 250,
            handCount: 0,
            hand: <String>[],
            finished: true,
            finishPosition: 1,
          ),
          _player(
            id: 'AI-1',
            score: 230,
            handCount: 2,
            hand: <String>['7O', '5R'],
          ),
          _player(
            id: 'AI-2',
            score: 240,
            handCount: 0,
            hand: <String>[],
            finished: true,
            finishPosition: 2,
          ),
        ],
        trick: <Map<String, Object?>>[],
        possibleActions: <Map<String, Object?>>[],
        appliedMoves: <Map<String, Object?>>[
          _move(playerId: 'p1', desc: 'SINGLE[10Y]'),
        ],
        previousRound: <String, Object?>{
          'roundNumber': 1,
          'winnerPlayerName': 'p1',
          'finishingOrderPlayerNames': <String>['p1', 'AI-2', 'AI-1'],
          'haggisCards': <String>['J', 'Q', 'K'],
          'playerScores': <Map<String, Object?>>[
            _previousRoundPlayerScore(
              playerName: 'p1',
              tricksPoints: 40,
              opponentsRemainingCardsPoints: 10,
              haggisPoints: 0,
              roundPoints: 50,
            ),
            _previousRoundPlayerScore(
              playerName: 'AI-1',
              tricksPoints: 0,
              opponentsRemainingCardsPoints: -10,
              haggisPoints: 0,
              roundPoints: -10,
            ),
            _previousRoundPlayerScore(
              playerName: 'AI-2',
              tricksPoints: 0,
              opponentsRemainingCardsPoints: -40,
              haggisPoints: 0,
              roundPoints: -40,
            ),
          ],
        },
      ),
    );

    await Future<void>.delayed(const Duration(milliseconds: 950));

    expect(controller.roundOverController.hasLastRound, isTrue);
    expect(controller.roundOverController.lastRound?.roundNumber, 1);
    expect(
      controller.roundOverController.lastRound?.status,
      'Round 1 finished. Game over.',
    );
  });
}

Map<String, dynamic> _stateMessage({
  required int version,
  required int roundNumber,
  required String currentPlayerId,
  required bool roundOver,
  required bool gameOver,
  required List<Map<String, Object?>> players,
  required List<Map<String, Object?>> trick,
  required List<Map<String, Object?>> possibleActions,
  required List<Map<String, Object?>> appliedMoves,
  Map<String, Object?>? previousRound,
}) {
  return <String, dynamic>{
    'type': 'CommandApplied',
    'command': <String, dynamic>{'type': 'Pass', 'playerId': 'p1'},
    'state': <String, dynamic>{
      'version': version,
      'data': <String, dynamic>{
        'roundNumber': roundNumber,
        'currentPlayerId': currentPlayerId,
        'roundOver': roundOver,
        'gameOver': gameOver,
        'players': players,
        'trick': trick,
        'possibleActions': possibleActions,
        'appliedMoves': appliedMoves,
        'previousRound': previousRound,
      },
    },
  };
}

Map<String, Object?> _player({
  required String id,
  required int score,
  required int handCount,
  required List<String> hand,
  bool finished = false,
  int finishPosition = 0,
}) {
  return <String, Object?>{
    'id': id,
    'score': score,
    'handCount': handCount,
    'hand': hand,
    'finished': finished,
    'finishPosition': finishPosition,
  };
}

Map<String, Object?> _move({
  required String playerId,
  required String desc,
  bool isPass = false,
}) {
  return <String, Object?>{
    'playerId': playerId,
    'desc': desc,
    'isPass': isPass,
  };
}

Map<String, Object?> _possibleAction({
  required String type,
  required String action,
}) {
  return <String, Object?>{'type': type, 'action': action};
}

Map<String, Object?> _previousRoundPlayerScore({
  required String playerName,
  required int tricksPoints,
  required int opponentsRemainingCardsPoints,
  required int haggisPoints,
  required int roundPoints,
}) {
  return <String, Object?>{
    'playerName': playerName,
    'tricksPoints': tricksPoints,
    'opponentsRemainingCardsPoints': opponentsRemainingCardsPoints,
    'haggisPoints': haggisPoints,
    'roundPoints': roundPoints,
  };
}

GameController _createControllerWithSnapshot({
  required List<String> hand,
  required List<String> possibleActions,
}) {
  final controller = GameController(
    serverBaseUrl: 'http://localhost:6666',
    playerId: 'p1',
    room: LobbyRoom(
      roomId: 'room-1',
      gameId: 'game-1',
      roomName: 'Test Room',
      players: <String>['p1', 'p2'],
    ),
  );

  controller.applySnapshotForTesting(
    GameSnapshot(
      version: 1,
      roundNumber: 1,
      currentPlayerId: 'p1',
      roundOver: false,
      gameOver: false,
      players: <GamePlayer>[
        GamePlayer(
          id: 'p1',
          score: 0,
          handCount: hand.length,
          hand: hand,
          finished: false,
          finishPosition: 0,
        ),
        GamePlayer(
          id: 'p2',
          score: 0,
          handCount: 0,
          hand: const <String>[],
          finished: false,
          finishPosition: 0,
        ),
      ],
      trick: const <TrickMove>[],
      possibleActions: possibleActions
          .map((action) => PossibleAction(type: 'Play', displayAction: action))
          .toList(growable: false),
      appliedMoves: const <TrickMove>[],
      previousRound: null,
    ),
  );

  return controller;
}
