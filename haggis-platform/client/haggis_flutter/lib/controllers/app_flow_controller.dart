import 'package:flutter/material.dart';

import '../app/app_settings.dart';
import '../models/lobby_models.dart';
import '../view_models/app_flow_view_model.dart';
import 'game_controller.dart';
import 'lobby_controller.dart';

class AppFlowController extends ChangeNotifier {
  AppFlowController({required AppSettings appSettings})
      : _config = ConnectionConfig(
          playerId: 'piotr',
          serverBaseUrl: appSettings.resolveServerBaseUrl(),
        );

  final ConnectionConfig _config;
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

  Future<void> connectToLobby() async {
    final controller = LobbyController(_config.serverBaseUrl, _config.playerId);
    await controller.connect();
    _lobbyController?.dispose();
    _lobbyController = controller;
    _screen = AppScreen.lobby;
    notifyListeners();
  }

  Future<void> openGame(LobbyRoom room) async {
    final controller = GameController(
      serverBaseUrl: _config.serverBaseUrl,
      playerId: _config.playerId,
      room: room,
    );
    await controller.connect();
    _gameController?.dispose();
    _gameController = controller;
    _screen = AppScreen.game;
    notifyListeners();
  }

  void leaveGame() {
    _gameController?.dispose();
    _gameController = null;
    _screen = AppScreen.lobby;
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

  @override
  void dispose() {
    _gameController?.dispose();
    _lobbyController?.dispose();
    super.dispose();
  }
}
