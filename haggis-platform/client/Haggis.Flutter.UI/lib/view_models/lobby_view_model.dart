import '../models/lobby_models.dart';

class LobbyViewModel {
  const LobbyViewModel({
    required this.playerId,
    required this.status,
    required this.rooms,
    required this.messages,
    required this.isBusy,
  });

  final String playerId;
  final String status;
  final List<LobbyRoom> rooms;
  final List<LobbyChatMessage> messages;
  final bool isBusy;
}
