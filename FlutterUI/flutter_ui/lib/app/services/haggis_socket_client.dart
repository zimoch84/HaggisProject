import 'dart:async';
import 'dart:convert';

import 'package:web_socket_channel/web_socket_channel.dart';

typedef SocketMessageHandler = void Function(String path, String message);
typedef SocketErrorHandler = void Function(String path, Object error);

class HaggisSocketClient {
  HaggisSocketClient({
    required this.host,
    required this.port,
    required this.onMessage,
    required this.onError,
  });

  final String host;
  final String port;
  final SocketMessageHandler onMessage;
  final SocketErrorHandler onError;

  final Map<String, WebSocketChannel> _channels = <String, WebSocketChannel>{};
  final Map<String, StreamSubscription<dynamic>> _subscriptions =
      <String, StreamSubscription<dynamic>>{};

  Future<void> send({
    required String path,
    required Map<String, dynamic> message,
  }) async {
    final channel = _channels[path] ?? _connect(path);
    channel.sink.add(jsonEncode(message));
  }

  WebSocketChannel _connect(String path) {
    final uri = Uri.parse('ws://$host:$port$path');
    final channel = WebSocketChannel.connect(uri);
    _channels[path] = channel;
    _subscriptions[path] = channel.stream.listen(
      (dynamic data) => onMessage(path, data.toString()),
      onError: (Object error) => onError(path, error),
      onDone: () {
        _subscriptions.remove(path)?.cancel();
        _channels.remove(path);
      },
    );
    return channel;
  }

  Future<void> dispose() async {
    for (final sub in _subscriptions.values) {
      await sub.cancel();
    }
    _subscriptions.clear();

    for (final channel in _channels.values) {
      await channel.sink.close();
    }
    _channels.clear();
  }
}
