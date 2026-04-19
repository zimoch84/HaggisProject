import 'package:shared_preferences/shared_preferences.dart';

class PlayerPreferences {
  static const String _playerIdKey = 'player_id';

  Future<String?> loadPlayerId() async {
    final preferences = await SharedPreferences.getInstance();
    final playerId = preferences.getString(_playerIdKey)?.trim();
    if (playerId == null || playerId.isEmpty) {
      return null;
    }
    return playerId;
  }

  Future<void> savePlayerId(String playerId) async {
    final normalized = playerId.trim();
    final preferences = await SharedPreferences.getInstance();
    if (normalized.isEmpty) {
      await preferences.remove(_playerIdKey);
      return;
    }
    await preferences.setString(_playerIdKey, normalized);
  }
}
