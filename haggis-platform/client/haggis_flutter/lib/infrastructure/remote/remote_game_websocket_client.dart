import 'dart:async';
import 'dart:convert';

import 'package:web_socket_channel/web_socket_channel.dart';

import '../../utils/ws_uri.dart';

class RemoteGameWebSocketClient {
  RemoteGameWebSocketClient({
    required this.serverBaseUrl,
    required this.gameId,
  });

  final String serverBaseUrl;
  final String gameId;
  final StreamController<Map<String, dynamic>> _messages =
      StreamController<Map<String, dynamic>>.broadcast();

  late final WebSocketChannel _channel;
  StreamSubscription<dynamic>? _subscription;

  Stream<Map<String, dynamic>> get messages => _messages.stream;

  Future<void> connect() async {
    _channel = WebSocketChannel.connect(
      buildWsUri(serverBaseUrl, '/ws/games/$gameId'),
    );
    _subscription = _channel.stream.listen((dynamic event) {
      _messages.add(jsonDecode(event as String) as Map<String, dynamic>);
    }, onError: _messages.addError, onDone: _messages.close);
  }

  void join(String playerId) {
    send({
      'operation': 'join',
      'payload': {'playerId': playerId},
    });
  }

  void requestSnapshot(String playerId) {
    send({
      'operation': 'snapshot',
      'payload': {'playerId': playerId},
    });
  }

  void createGame(String playerId, int playerCount) {
    send({
      'operation': 'create',
      'payload': {
        'playerId': playerId,
        'payload': {
          'playerCount': playerCount,
        },
      },
    });
  }

  void sendPass(String playerId) {
    send({
      'operation': 'command',
      'payload': {
        'command': {
          'type': 'Pass',
          'playerId': playerId,
          'payload': <String, Object?>{},
        },
      },
    });
  }

  void sendPlay(String playerId, String action) {
    send({
      'operation': 'command',
      'payload': {
        'command': {
          'type': 'Play',
          'playerId': playerId,
          'payload': {
            'action': action,
          },
        },
      },
    });
  }

  void send(Map<String, Object?> payload) {
    _channel.sink.add(jsonEncode(payload));
  }

  Future<void> dispose() async {
    await _subscription?.cancel();
    await _channel.sink.close();
  }
}
