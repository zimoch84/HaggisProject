import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/app/app_settings.dart';
import 'package:haggis_flutter/app/player_preferences.dart';
import 'package:haggis_flutter/controllers/app_flow_controller.dart';
import 'package:haggis_flutter/controllers/game_controller.dart';
import 'package:haggis_flutter/controllers/lobby_controller.dart';
import 'package:haggis_flutter/models/lobby_models.dart';
import 'package:haggis_flutter/models/single_player_models.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('AppSettings candidate urls', () {
    test('returns Android fallback chain on Android', () {
      final settings = AppSettings(
        serverBaseUrl: 'http://localhost:6666',
        androidServerBaseUrls: const <String>[
          'http://192.168.0.17:6666',
          'http://10.0.2.2:6666',
        ],
      );

      expect(
        settings.resolveCandidateServerBaseUrls(
          isWebOverride: false,
          targetPlatformOverride: TargetPlatform.android,
        ),
        const <String>[
          'http://192.168.0.17:6666',
          'http://10.0.2.2:6666',
        ],
      );
    });

    test('returns desktop localhost outside Android', () {
      final settings = AppSettings(
        serverBaseUrl: 'http://localhost:6666',
        androidServerBaseUrls: const <String>[
          'http://192.168.0.17:6666',
          'http://10.0.2.2:6666',
        ],
      );

      expect(
        settings.resolveCandidateServerBaseUrls(
          isWebOverride: false,
          targetPlatformOverride: TargetPlatform.windows,
        ),
        const <String>['http://localhost:6666'],
      );
    });
  });

  group('AppFlowController server fallback', () {
    setUp(() {
      SharedPreferences.setMockInitialValues(<String, Object>{});
      debugDefaultTargetPlatformOverride = TargetPlatform.android;
    });

    tearDown(() {
      debugDefaultTargetPlatformOverride = null;
    });

    test('uses first Android address when probe succeeds', () async {
      final probedUrls = <String>[];
      final createdLobbyUrls = <String>[];
      final controller = AppFlowController(
        appSettings: _settings(),
        playerPreferences: PlayerPreferences(),
        serverProbe: (String serverBaseUrl) async {
          probedUrls.add(serverBaseUrl);
        },
        lobbyControllerFactory: (String serverBaseUrl, String playerId) {
          createdLobbyUrls.add(serverBaseUrl);
          return _FakeLobbyController(serverBaseUrl, playerId);
        },
      );

      controller.updatePlayerId('p1');
      await controller.connectToLobby();

      expect(probedUrls, const <String>['http://192.168.0.17:6666']);
      expect(createdLobbyUrls, const <String>['http://192.168.0.17:6666']);
      expect(
        controller.viewModel.serverBaseUrl,
        'http://192.168.0.17:6666',
      );
      expect(controller.lobbyController, isNotNull);
    });

    test('falls back to emulator address when LAN probe fails', () async {
      final probedUrls = <String>[];
      final createdGameUrls = <String>[];
      final controller = AppFlowController(
        appSettings: _settings(),
        playerPreferences: PlayerPreferences(),
        serverProbe: (String serverBaseUrl) async {
          probedUrls.add(serverBaseUrl);
          if (serverBaseUrl == 'http://192.168.0.17:6666') {
            throw Exception('LAN unavailable');
          }
        },
        gameControllerFactory: ({
          required String serverBaseUrl,
          required String playerId,
          required LobbyRoom room,
          bool singlePlayer = false,
          List<SinglePlayerAiConfig> singlePlayerAiPlayers =
              const <SinglePlayerAiConfig>[],
        }) {
          createdGameUrls.add(serverBaseUrl);
          return _FakeGameController(
            serverBaseUrl: serverBaseUrl,
            playerId: playerId,
            room: room,
            singlePlayer: singlePlayer,
            singlePlayerAiPlayers: singlePlayerAiPlayers,
          );
        },
      );

      controller.updatePlayerId('p1');
      await controller.openSinglePlayerGame(
        const <SinglePlayerAiConfig>[
          SinglePlayerAiConfig(name: 'AI-1', difficulty: AiDifficulty.normal),
          SinglePlayerAiConfig(name: 'AI-2', difficulty: AiDifficulty.hard),
        ],
      );

      expect(
        probedUrls,
        const <String>[
          'http://192.168.0.17:6666',
          'http://10.0.2.2:6666',
        ],
      );
      expect(createdGameUrls, const <String>['http://10.0.2.2:6666']);
      expect(controller.viewModel.serverBaseUrl, 'http://10.0.2.2:6666');
      expect(controller.gameController, isNotNull);
    });

    test('reports all attempted Android addresses when probes fail', () async {
      final controller = AppFlowController(
        appSettings: _settings(),
        playerPreferences: PlayerPreferences(),
        serverProbe: (String serverBaseUrl) async {
          throw Exception('No route to $serverBaseUrl');
        },
        lobbyControllerFactory: (String serverBaseUrl, String playerId) =>
            _FakeLobbyController(serverBaseUrl, playerId),
      );

      controller.updatePlayerId('p1');

      await expectLater(
        controller.connectToLobby(),
        throwsA(
          isA<Exception>().having(
            (Exception error) => error.toString(),
            'message',
            allOf(
              contains('http://192.168.0.17:6666'),
              contains('http://10.0.2.2:6666'),
            ),
          ),
        ),
      );
    });
  });
}

AppSettings _settings() {
  return AppSettings(
    serverBaseUrl: 'http://localhost:6666',
    androidServerBaseUrls: const <String>[
      'http://192.168.0.17:6666',
      'http://10.0.2.2:6666',
    ],
  );
}

class _FakeLobbyController extends LobbyController {
  _FakeLobbyController(super.serverBaseUrl, super.playerId);

  @override
  Future<void> connect() async {}
}

class _FakeGameController extends GameController {
  _FakeGameController({
    required super.serverBaseUrl,
    required super.playerId,
    required super.room,
    super.singlePlayer,
    super.singlePlayerAiPlayers,
  });

  @override
  Future<void> connect() async {}
}
