import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/controllers/game_controller.dart';
import 'package:haggis_flutter/models/game_models.dart';
import 'package:haggis_flutter/models/lobby_models.dart';

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
