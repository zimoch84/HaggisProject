import 'package:flutter/material.dart';
import 'package:web_socket_channel/web_socket_channel.dart';

import '../app/app_settings.dart';
import '../app/player_preferences.dart';
import '../infrastructure/logging/app_logger.dart';
import '../models/lobby_models.dart';
import '../models/single_player_models.dart';
import '../utils/ws_uri.dart';
import '../view_models/app_flow_view_model.dart';
import 'game_controller.dart';
import 'lobby_controller.dart';

typedef LobbyControllerFactory =
    LobbyController Function(String serverBaseUrl, String playerId);
typedef GameControllerFactory =
    GameController Function({
      required String serverBaseUrl,
      required String playerId,
      required LobbyRoom room,
      bool singlePlayer,
      List<SinglePlayerAiConfig> singlePlayerAiPlayers,
    });
typedef ServerProbe = Future<void> Function(String serverBaseUrl);

class AppFlowController extends ChangeNotifier {
  AppFlowController({
    required AppSettings appSettings,
    required PlayerPreferences playerPreferences,
    LobbyControllerFactory? lobbyControllerFactory,
    GameControllerFactory? gameControllerFactory,
    ServerProbe? serverProbe,
  }) : _config = ConnectionConfig(
         playerId: '',
         serverBaseUrl: appSettings.resolveServerBaseUrl(),
       ),
       _playerPreferences = playerPreferences,
       _appSettings = appSettings,
       _lobbyControllerFactory =
           lobbyControllerFactory ?? _defaultLobbyControllerFactory,
       _gameControllerFactory =
           gameControllerFactory ?? _defaultGameControllerFactory,
       _serverProbe = serverProbe ?? _defaultServerProbe;

  final ConnectionConfig _config;
  final PlayerPreferences _playerPreferences;
  final AppSettings _appSettings;
  final LobbyControllerFactory _lobbyControllerFactory;
  final GameControllerFactory _gameControllerFactory;
  final ServerProbe _serverProbe;
  AppScreen _screen = AppScreen.connect;
  LobbyController? _lobbyController;
  GameController? _gameController;

  AppFlowViewModel get viewModel => AppFlowViewModel(
    screen: _screen,
    playerId: _config.playerId,
    serverBaseUrl: _config.serverBaseUrl,
  );

  LobbyController? get lobbyController => _lobbyController;

  GameController? get gameController => _gameController;

  void updatePlayerId(String playerId) {
    _config.playerId = playerId;
    AppLogger.info('AppFlow', 'Updated playerId to "$playerId".');
    notifyListeners();
  }

  Future<bool> tryRestoreSession() async {
    final savedPlayerId = await _playerPreferences.loadPlayerId();
    if (savedPlayerId == null) {
      AppLogger.info('AppFlow', 'No saved playerId found. Staying on connect.');
      _screen = AppScreen.connect;
      notifyListeners();
      return false;
    }

    _config.playerId = savedPlayerId;
    AppLogger.info('AppFlow', 'Restored saved playerId "$savedPlayerId".');
    _screen = AppScreen.modeSelect;
    notifyListeners();
    return true;
  }

  Future<void> enterModeSelect() async {
    await _playerPreferences.savePlayerId(_config.playerId);
    AppLogger.info(
      'AppFlow',
      'Entered mode select with serverBaseUrl=${_config.serverBaseUrl}.',
    );
    _screen = AppScreen.modeSelect;
    notifyListeners();
  }

  Future<void> connectToLobby() async {
    final serverBaseUrl = await _resolveActiveServerBaseUrl();
    AppLogger.info('AppFlow', 'Connecting to lobby via $serverBaseUrl.');
    final controller = _lobbyControllerFactory(serverBaseUrl, _config.playerId);
    try {
      await controller.connect().timeout(const Duration(seconds: 5));
      _config.serverBaseUrl = serverBaseUrl;
      await _playerPreferences.savePlayerId(_config.playerId);
      _lobbyController?.dispose();
      _lobbyController = controller;
      _screen = AppScreen.lobby;
      AppLogger.info(
        'AppFlow',
        'Lobby connection established via $serverBaseUrl.',
      );
      notifyListeners();
    } catch (error) {
      AppLogger.error(
        'AppFlow',
        'Lobby connection failed via $serverBaseUrl.',
        error,
      );
      controller.dispose();
      rethrow;
    }
  }

  void openSinglePlayerSetup() {
    _screen = AppScreen.singlePlayerSetup;
    notifyListeners();
  }

  void backToModeSelect() {
    _screen = AppScreen.modeSelect;
    notifyListeners();
  }

  Future<void> openSinglePlayerGame(
    List<SinglePlayerAiConfig> aiPlayers,
  ) async {
    final gameId = _buildSinglePlayerGameId();
    final room = LobbyRoom(
      roomId: gameId,
      gameId: gameId,
      roomName: 'Single Player',
      players: <String>[
        _config.playerId,
        ...aiPlayers.map((SinglePlayerAiConfig config) => config.name),
      ],
    );
    await openGame(room, singlePlayer: true, aiPlayers: aiPlayers);
  }

  Future<void> openGame(
    LobbyRoom room, {
    bool singlePlayer = false,
    List<SinglePlayerAiConfig> aiPlayers = const <SinglePlayerAiConfig>[],
  }) async {
    final serverBaseUrl = singlePlayer
        ? _config.serverBaseUrl
        : _lobbyController != null
        ? _config.serverBaseUrl
        : await _resolveActiveServerBaseUrl();
    AppLogger.info(
      'AppFlow',
      'Opening game ${room.gameId} via $serverBaseUrl. singlePlayer=$singlePlayer',
    );
    final controller = _gameControllerFactory(
      serverBaseUrl: serverBaseUrl,
      playerId: _config.playerId,
      room: room,
      singlePlayer: singlePlayer,
      singlePlayerAiPlayers: aiPlayers,
    );
    try {
      await controller.connect();
    } catch (error) {
      AppLogger.error(
        'AppFlow',
        'Game connection failed for ${room.gameId} via $serverBaseUrl.',
        error,
      );
      controller.dispose();
      rethrow;
    }
    _config.serverBaseUrl = serverBaseUrl;
    _gameController?.dispose();
    _gameController = controller;
    _screen = AppScreen.game;
    AppLogger.info(
      'AppFlow',
      'Game connection established for ${room.gameId}.',
    );
    notifyListeners();
  }

  void leaveGame() {
    AppLogger.info('AppFlow', 'Leaving current game.');
    _gameController?.dispose();
    _gameController = null;
    _screen = AppScreen.modeSelect;
    notifyListeners();
  }

  void disconnectLobby() {
    AppLogger.info('AppFlow', 'Disconnecting lobby and game state.');
    _gameController?.dispose();
    _lobbyController?.dispose();
    _gameController = null;
    _lobbyController = null;
    _screen = AppScreen.connect;
    notifyListeners();
  }

  String _buildSinglePlayerGameId() {
    final safePlayerId = _config.playerId
        .trim()
        .toLowerCase()
        .replaceAll(RegExp(r'[^a-z0-9]+'), '-')
        .replaceAll(RegExp(r'^-+|-+$'), '');
    final suffix = DateTime.now().millisecondsSinceEpoch;
    return 'single-${safePlayerId.isEmpty ? 'player' : safePlayerId}-$suffix';
  }

  @override
  void dispose() {
    _gameController?.dispose();
    _lobbyController?.dispose();
    super.dispose();
  }

  Future<String> _resolveActiveServerBaseUrl() async {
    final candidates = _orderedCandidateServerBaseUrls();
    Object? lastError;
    AppLogger.info(
      'AppFlow',
      'Resolving active backend URL. Candidates: ${candidates.join(', ')}',
    );

    for (final String candidate in candidates) {
      try {
        AppLogger.info('AppFlow', 'Probing backend candidate $candidate.');
        await _serverProbe(candidate).timeout(const Duration(seconds: 2));
        AppLogger.info('AppFlow', 'Backend candidate $candidate is reachable.');
        return candidate;
      } catch (error) {
        lastError = error;
        AppLogger.warn(
          'AppFlow',
          'Backend candidate $candidate failed: $error',
        );
      }
    }

    final attempted = candidates.join(', ');
    final suffix = lastError == null ? '' : ' Last error: $lastError';
    throw Exception('Could not connect to backend. Tried: $attempted.$suffix');
  }

  List<String> _orderedCandidateServerBaseUrls() {
    final candidates = _appSettings.resolveCandidateServerBaseUrls();
    final current = _config.serverBaseUrl.trim();
    final ordered = <String>[];

    if (current.isNotEmpty) {
      ordered.add(current);
    }

    for (final String candidate in candidates) {
      if (!ordered.contains(candidate)) {
        ordered.add(candidate);
      }
    }

    return ordered;
  }
}

LobbyController _defaultLobbyControllerFactory(
  String serverBaseUrl,
  String playerId,
) {
  return LobbyController(serverBaseUrl, playerId);
}

GameController _defaultGameControllerFactory({
  required String serverBaseUrl,
  required String playerId,
  required LobbyRoom room,
  bool singlePlayer = false,
  List<SinglePlayerAiConfig> singlePlayerAiPlayers =
      const <SinglePlayerAiConfig>[],
}) {
  return GameController(
    serverBaseUrl: serverBaseUrl,
    playerId: playerId,
    room: room,
    singlePlayer: singlePlayer,
    singlePlayerAiPlayers: singlePlayerAiPlayers,
  );
}

Future<void> _defaultServerProbe(String serverBaseUrl) async {
  AppLogger.info('AppFlow', 'Opening probe websocket to $serverBaseUrl.');
  final channel = WebSocketChannel.connect(
    buildWsUri(serverBaseUrl, '/ws/global/chat'),
  );

  try {
    await channel.ready.timeout(const Duration(seconds: 2));
    AppLogger.info('AppFlow', 'Probe websocket ready for $serverBaseUrl.');
  } finally {
    AppLogger.info('AppFlow', 'Closing probe websocket for $serverBaseUrl.');
    await channel.sink.close();
  }
}
