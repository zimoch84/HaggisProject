import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:web_socket_channel/web_socket_channel.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return const MaterialApp(
      debugShowCheckedModeBanner: false,
      home: HomePage(),
    );
  }
}

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  WebSocketChannel? _channel;
  StreamSubscription<dynamic>? _subscription;
  String _status = 'Not connected';
  String _message = 'Nacisnij przycisk, aby utworzyc nowa gre.';
  String? _gameId;
  String _lastRequestPreview = '';

  @override
  void initState() {
    super.initState();
  }

  @override
  void dispose() {
    _subscription?.cancel();
    _channel?.sink.close();
    super.dispose();
  }

  Future<void> _createGameAndSendCommand() async {
    final request = _buildInitializeRequest();
    final preview = const JsonEncoder.withIndent('  ').convert(request);
    final gameId = 'game-${DateTime.now().millisecondsSinceEpoch}';

    final shouldSend = await showDialog<bool>(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: const Text('Request JSON (przed wyslaniem)'),
          content: SingleChildScrollView(
            child: SelectableText(preview),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(context).pop(false),
              child: const Text('Anuluj'),
            ),
            ElevatedButton(
              onPressed: () => Navigator.of(context).pop(true),
              child: const Text('Wyslij'),
            ),
          ],
        );
      },
    );

    if (shouldSend != true) {
      return;
    }

    _subscription?.cancel();
    _channel?.sink.close();

    final channel = WebSocketChannel.connect(
      Uri.parse('ws://10.0.2.2:5135/ws/games/$gameId'),
    );

    _channel = channel;
    _gameId = gameId;
    _lastRequestPreview = preview;
    _status = 'Connecting...';
    _message = 'Tworzenie gry: $gameId';
    setState(() {});

    _subscription = channel.stream.listen(
      (dynamic data) {
        setState(() {
          _status = 'Connected';
          _message = _extractSummary(data.toString());
        });
      },
      onError: (Object error) {
        setState(() {
          _status = 'Connection error';
          _message = error.toString();
        });
      },
      onDone: () {
        setState(() {
          _status = 'Disconnected';
        });
      },
    );

    channel.sink.add(jsonEncode(request));
  }

  Map<String, dynamic> _buildInitializeRequest() {
    return {
      'type': 'Command',
      'command': {
        'type': 'Initialize',
        'playerId': 'piotr',
        'payload': {
          'players': ['piotr', 'bot']
        }
      }
    };
  }

  String _extractSummary(String raw) {
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! Map<String, dynamic>) {
        return raw;
      }

      final type = decoded['type'] ?? 'Unknown';
      final gameId = decoded['gameId'] ?? _gameId ?? 'unknown';
      return 'Event: $type\nGameId: $gameId\n$raw';
    } catch (_) {
      return raw;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('WebSocket Demo')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text('Status: $_status'),
              const SizedBox(height: 12),
              ElevatedButton(
                onPressed: _createGameAndSendCommand,
                child: const Text('Utworz nowa gre i wyslij Initialize'),
              ),
              const SizedBox(height: 12),
              if (_gameId != null) Text('GameId: $_gameId'),
              const SizedBox(height: 12),
              if (_lastRequestPreview.isNotEmpty)
                SelectableText(
                  'Ostatni request:\n$_lastRequestPreview',
                ),
              const SizedBox(height: 12),
              Text(
                _message,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 16),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
