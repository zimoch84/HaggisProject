import 'package:flutter/material.dart';

import '../app/app_settings.dart';
import '../app/player_preferences.dart';
import '../models/lobby_models.dart';
import '../models/single_player_models.dart';
import '../view_models/app_flow_view_model.dart';
import 'game_controller.dart';
import 'lobby_controller.dart';

class AppFlowController extends ChangeNotifier {
  AppFlowController({
    required AppSettings appSettings,
    required PlayerPreferences playerPreferences,
  }) : _config = ConnectionConfig(
         playerId: '',
         serverBaseUrl: appSettings.resolveServerBaseUrl(),
       ),
       _playerPreferences = playerPreferences;

  final ConnectionConfig _config;
  final PlayerPreferences _playerPreferences;
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
    notifyListeners();
  }

  Future<bool> tryRestoreSession() async {
    final savedPlayerId = await _playerPreferences.loadPlayerId();
    if (savedPlayerId == null) {
      _screen = AppScreen.connect;
      notifyListeners();
      return false;
    }

    _config.playerId = savedPlayerId;
    _screen = AppScreen.modeSelect;
    notifyListeners();
    return true;
  }

  Future<void> enterModeSelect() async {
    await _playerPreferences.savePlayerId(_config.playerId);
    _screen = AppScreen.modeSelect;
    notifyListeners();
  }

  Future<void> connectToLobby() async {
    final controller = LobbyController(_config.serverBaseUrl, _config.playerId);
    try {
      await controller.connect().timeout(const Duration(seconds: 5));
      await _playerPreferences.savePlayerId(_config.playerId);
      _lobbyController?.dispose();
      _lobbyController = controller;
      _screen = AppScreen.lobby;
      notifyListeners();
    } catch (_) {
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
    final controller = GameController(
      serverBaseUrl: _config.serverBaseUrl,
      playerId: _config.playerId,
      room: room,
      singlePlayer: singlePlayer,
      singlePlayerAiPlayers: aiPlayers,
    );
    try {
      await controller.connect();
    } catch (_) {
      controller.dispose();
      rethrow;
    }
    _gameController?.dispose();
    _gameController = controller;
    _screen = AppScreen.game;
    notifyListeners();
  }

  void leaveGame() {
    _gameController?.dispose();
    _gameController = null;
    _screen = AppScreen.modeSelect;
    notifyListeners();
  }

  void disconnectLobby() {
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
}
