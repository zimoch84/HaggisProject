class ConnectViewModel {
  const ConnectViewModel({
    required this.playerId,
    required this.serverBaseUrl,
    required this.isConnecting,
    required this.error,
  });

  final String playerId;
  final String serverBaseUrl;
  final bool isConnecting;
  final String? error;
}
