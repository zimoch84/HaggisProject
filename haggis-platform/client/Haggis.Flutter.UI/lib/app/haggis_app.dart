import 'package:flutter/material.dart';

import '../controllers/app_flow_controller.dart';
import '../models/lobby_models.dart';
import '../view_models/app_flow_view_model.dart';
import '../view_models/connect_view_model.dart';
import '../pages/connect_page.dart';
import '../pages/game_page.dart';
import '../pages/lobby_page.dart';
import 'app_build_info.dart';
import 'app_settings.dart';
import 'player_preferences.dart';

class HaggisFlutterApp extends StatelessWidget {
  const HaggisFlutterApp({super.key, required this.appSettings});

  final AppSettings appSettings;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'haggis',
      debugShowCheckedModeBanner: false,
      builder: (BuildContext context, Widget? child) {
        return Stack(
          children: [
            ?child,
            const Positioned(right: 8, bottom: 8, child: _AppBuildBadge()),
          ],
        );
      },
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

class _AppBuildBadge extends StatelessWidget {
  const _AppBuildBadge();

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: const Color(0xCC162A2E),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: const Color(0x6656B891)),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
          child: Text(
            AppBuildInfo.displayLabel,
            style: const TextStyle(
              color: Colors.white70,
              fontSize: 10,
              fontWeight: FontWeight.w700,
              decoration: TextDecoration.none,
            ),
          ),
        ),
      ),
    );
  }
}

class HaggisHomePage extends StatefulWidget {
  const HaggisHomePage({super.key, required this.appSettings});

  final AppSettings appSettings;

  @override
  State<HaggisHomePage> createState() => _HaggisHomePageState();
}

class _HaggisHomePageState extends State<HaggisHomePage> {
  late final AppFlowController _appFlowController;
  bool _bootstrapping = true;
  bool _connecting = false;
  String? _connectError;

  @override
  void initState() {
    super.initState();
    _appFlowController = AppFlowController(
      appSettings: widget.appSettings,
      playerPreferences: PlayerPreferences(),
    );
    _appFlowController.addListener(_refresh);
    _bootstrap();
  }

  @override
  void dispose() {
    _appFlowController.removeListener(_refresh);
    _appFlowController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_bootstrapping) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

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

  Future<void> _bootstrap() async {
    setState(() {
      _bootstrapping = true;
      _connectError = null;
    });

    try {
      await _appFlowController.tryRestoreSession();
    } catch (error) {
      setState(() {
        _connectError = error.toString();
      });
    } finally {
      if (mounted) {
        setState(() {
          _bootstrapping = false;
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
