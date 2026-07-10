import 'dart:async';
import 'dart:convert';

import 'package:web_socket_channel/web_socket_channel.dart';

import '../logging/app_logger.dart';
import '../../utils/ws_uri.dart';

class GlobalLobbyWebSocketClient {
  GlobalLobbyWebSocketClient(this.serverBaseUrl);

  final String serverBaseUrl;
  final StreamController<Map<String, dynamic>> _messages =
      StreamController<Map<String, dynamic>>.broadcast();

  late final WebSocketChannel _channel;
  StreamSubscription<dynamic>? _subscription;

  Stream<Map<String, dynamic>> get messages => _messages.stream;

  Future<void> connect() async {
    final uri = buildWsUri(serverBaseUrl, '/ws/global/chat');
    AppLogger.info('LobbyWs', 'Connecting to $uri.');
    _channel = WebSocketChannel.connect(
      uri,
    );
    await _channel.ready.timeout(const Duration(seconds: 5));
    AppLogger.info('LobbyWs', 'WebSocket ready: $uri');
    _subscription = _channel.stream.listen((dynamic event) {
      AppLogger.info('LobbyWs', 'Received frame: $event');
      _messages.add(jsonDecode(event as String) as Map<String, dynamic>);
    }, onError: (Object error, StackTrace stackTrace) {
      AppLogger.error('LobbyWs', 'WebSocket stream error.', error);
      _messages.addError(error, stackTrace);
    }, onDone: () {
      AppLogger.warn('LobbyWs', 'WebSocket stream closed by remote side.');
      _messages.close();
    });
  }

  void requestRoomList() {
    send({'operation': 'listroom'});
  }

  void createRoom({
    required String playerId,
    required String roomName,
    required String roomId,
  }) {
    send({
      'operation': 'createroom',
      'payload': {
        'playerId': playerId,
        'roomName': roomName,
        'roomId': roomId,
      },
    });
  }

  void send(Map<String, Object?> payload) {
    AppLogger.info('LobbyWs', 'Sending payload: ${jsonEncode(payload)}');
    _channel.sink.add(jsonEncode(payload));
  }

  Future<void> dispose() async {
    AppLogger.info('LobbyWs', 'Disposing websocket client for $serverBaseUrl.');
    await _subscription?.cancel();
    await _channel.sink.close();
  }
}
