enum AppScreen { connect, modeSelect, singlePlayerSetup, lobby, game }

class AppFlowViewModel {
  const AppFlowViewModel({
    required this.screen,
    required this.playerId,
    required this.serverBaseUrl,
  });

  final AppScreen screen;
  final String playerId;
  final String serverBaseUrl;
}
