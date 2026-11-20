import 'dart:async';
import 'dart:convert';

import 'package:web_socket_channel/web_socket_channel.dart';

import '../logging/app_logger.dart';
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
    final uri = buildWsUri(serverBaseUrl, '/ws/games/$gameId');
    AppLogger.info('GameWs', 'Connecting to $uri.');
    _channel = WebSocketChannel.connect(
      uri,
    );
    await _channel.ready.timeout(const Duration(seconds: 5));
    AppLogger.info('GameWs', 'WebSocket ready: $uri');
    _subscription = _channel.stream.listen(
      (dynamic event) {
        AppLogger.info('GameWs', 'Received frame: $event');
        _messages.add(jsonDecode(event as String) as Map<String, dynamic>);
      },
      onError: (Object error, StackTrace stackTrace) {
        AppLogger.error('GameWs', 'WebSocket stream error.', error);
        _messages.addError(error, stackTrace);
      },
      onDone: () {
        AppLogger.warn('GameWs', 'WebSocket stream closed by remote side.');
        _messages.close();
      },
    );
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

  void createGame(
    String playerId,
    int playerCount, {
    int? seed,
    List<Map<String, Object?>>? players,
  }) {
    final createPayload = <String, Object?>{
      'playerCount': playerCount,
      'seed': seed,
    };
    if (players != null) {
      createPayload['players'] = players;
    }

    send({
      'operation': 'create',
      'payload': {'playerId': playerId, 'payload': createPayload},
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
          'payload': {'action': action},
        },
      },
    });
  }

  void send(Map<String, Object?> payload) {
    AppLogger.info('GameWs', 'Sending payload: ${jsonEncode(payload)}');
    _channel.sink.add(jsonEncode(payload));
  }

  Future<void> dispose() async {
    AppLogger.info('GameWs', 'Disposing websocket client for $gameId.');
    await _subscription?.cancel();
    await _channel.sink.close();
  }
}
