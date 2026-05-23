import 'dart:async';
import 'dart:convert';

import 'package:web_socket_channel/web_socket_channel.dart';

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
    _channel = WebSocketChannel.connect(
      buildWsUri(serverBaseUrl, '/ws/global/chat'),
    );
    await _channel.ready.timeout(const Duration(seconds: 5));
    _subscription = _channel.stream.listen((dynamic event) {
      _messages.add(jsonDecode(event as String) as Map<String, dynamic>);
    }, onError: _messages.addError, onDone: _messages.close);
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
    _channel.sink.add(jsonEncode(payload));
  }

  Future<void> dispose() async {
    await _subscription?.cancel();
    await _channel.sink.close();
  }
}
