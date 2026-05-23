class LobbyRoom {
  LobbyRoom({
    required this.roomId,
    required this.gameId,
    required this.roomName,
    required this.players,
  });

  factory LobbyRoom.fromJson(Map<String, dynamic> json) {
    return LobbyRoom(
      roomId: (json['roomId'] ?? '').toString(),
      gameId: (json['gameId'] ?? '').toString(),
      roomName: (json['roomName'] ?? '').toString(),
      players: (json['players'] as List<dynamic>? ?? <dynamic>[])
          .map((dynamic item) => item.toString())
          .toList(),
    );
  }

  final String roomId;
  final String gameId;
  final String roomName;
  final List<String> players;

  String get displayName {
    final trimmedRoomName = roomName.trim();
    if (trimmedRoomName.isNotEmpty) {
      return trimmedRoomName;
    }

    final trimmedRoomId = roomId.trim();
    if (trimmedRoomId.isNotEmpty) {
      return trimmedRoomId;
    }

    return gameId.trim();
  }
}

class LobbyChatMessage {
  LobbyChatMessage({required this.playerId, required this.text});

  factory LobbyChatMessage.fromJson(Map<String, dynamic> json) {
    return LobbyChatMessage(
      playerId: (json['playerId'] ?? '').toString(),
      text: (json['text'] ?? '').toString(),
    );
  }

  final String playerId;
  final String text;
}
