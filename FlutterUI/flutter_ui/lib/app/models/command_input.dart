class CommandInput {
  const CommandInput({
    required this.playerId,
    required this.text,
    required this.targetPlayerId,
    required this.roomName,
    required this.roomId,
    required this.gameId,
    required this.commandType,
  });

  final String playerId;
  final String text;
  final String targetPlayerId;
  final String roomName;
  final String roomId;
  final String gameId;
  final String commandType;
}
