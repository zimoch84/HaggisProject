import 'package:flutter/material.dart';

import '../controllers/app_flow_controller.dart';
import '../models/lobby_models.dart';
import '../view_models/app_flow_view_model.dart';
import '../view_models/connect_view_model.dart';
import '../pages/connect_page.dart';
import '../pages/game_page.dart';
import '../pages/lobby_page.dart';
import 'app_settings.dart';

class HaggisFlutterApp extends StatelessWidget {
  const HaggisFlutterApp({
    super.key,
    required this.appSettings,
  });

  final AppSettings appSettings;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'haggis',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFFB54A2E),
          brightness: Brightness.light,
        ),
        scaffoldBackgroundColor: const Color(0xFFF5EFE4),
        useMaterial3: true,
      ),
      home: HaggisHomePage(appSettings: appSettings),
    );
  }
}

class HaggisHomePage extends StatefulWidget {
  const HaggisHomePage({
    super.key,
    required this.appSettings,
  });

  final AppSettings appSettings;

  @override
  State<HaggisHomePage> createState() => _HaggisHomePageState();
}

class _HaggisHomePageState extends State<HaggisHomePage> {
  late final AppFlowController _appFlowController;
  bool _connecting = false;
  String? _connectError;

  @override
  void initState() {
    super.initState();
    _appFlowController = AppFlowController(appSettings: widget.appSettings);
    _appFlowController.addListener(_refresh);
  }

  @override
  void dispose() {
    _appFlowController.removeListener(_refresh);
    _appFlowController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final flow = _appFlowController.viewModel;

    switch (flow.screen) {
      case AppScreen.connect:
        return ConnectPage(
          viewModel: ConnectViewModel(
            playerId: flow.playerId,
            serverBaseUrl: flow.serverBaseUrl,
            isConnecting: _connecting,
            error: _connectError,
          ),
          onPlayerIdChanged: _appFlowController.updatePlayerId,
          onConnect: _connectToLobby,
        );
      case AppScreen.lobby:
        final controller = _appFlowController.lobbyController;
        if (controller == null) {
          return const SizedBox.shrink();
        }
        return LobbyPage(
          controller: controller,
          onJoinRoom: _openGame,
          onDisconnect: _appFlowController.disconnectLobby,
        );
      case AppScreen.game:
        final controller = _appFlowController.gameController;
        if (controller == null) {
          return const SizedBox.shrink();
        }
        return GamePage(
          controller: controller,
          onLeave: _appFlowController.leaveGame,
        );
    }
  }

  Future<void> _connectToLobby() async {
    setState(() {
      _connecting = true;
      _connectError = null;
    });

    try {
      await _appFlowController.connectToLobby();
    } catch (error) {
      setState(() {
        _connectError = error.toString();
      });
    } finally {
      if (mounted) {
        setState(() {
          _connecting = false;
        });
      }
    }
  }

  Future<void> _openGame(LobbyRoom room) async {
    await _appFlowController.openGame(room);
  }

  void _refresh() {
    if (mounted) {
      setState(() {});
    }
  }
}
