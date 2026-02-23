import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:web_socket_channel/web_socket_channel.dart';

void main() {
  runApp(const HaggisApp());
}

class HaggisApp extends StatelessWidget {
  const HaggisApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Haggis',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF2A6F58)),
        scaffoldBackgroundColor: const Color(0xFF113D34),
      ),
      home: const GameHomePage(),
    );
  }
}

class GameHomePage extends StatefulWidget {
  const GameHomePage({super.key});

  @override
  State<GameHomePage> createState() => _GameHomePageState();
}

class _GameHomePageState extends State<GameHomePage> {
  WebSocketChannel? _gameChannel;
  StreamSubscription<dynamic>? _gameSub;
  String _gameStatus = 'Brak aktywnej gry';
  String? _gameId;

  String get _gameApiHost {
    if (kIsWeb) {
      return 'localhost';
    }
    if (defaultTargetPlatform == TargetPlatform.android) {
      return '10.0.2.2';
    }
    return 'localhost';
  }

  @override
  void dispose() {
    _gameSub?.cancel();
    _gameChannel?.sink.close();
    super.dispose();
  }

  Future<void> _createGame() async {
    _gameSub?.cancel();
    _gameChannel?.sink.close();

    final gameId = 'game-${DateTime.now().millisecondsSinceEpoch}';
    final uri = Uri.parse('ws://$_gameApiHost:5135/ws/games/$gameId');
    final channel = WebSocketChannel.connect(uri);
    _gameChannel = channel;

    setState(() {
      _gameId = gameId;
      _gameStatus = 'Laczenie z GameAPI...';
    });

    _gameSub = channel.stream.listen(
      (dynamic data) {
        setState(() {
          _gameStatus = 'Gra utworzona. Odpowiedz: ${data.toString()}';
        });
      },
      onError: (Object error) {
        setState(() {
          _gameStatus = 'Blad GameAPI: $error';
        });
      },
      onDone: () {
        setState(() {
          if (_gameStatus.startsWith('Laczenie')) {
            _gameStatus = 'Polaczenie z GameAPI zamkniete';
          }
        });
      },
    );

    final payload = <String, dynamic>{
      'type': 'Command',
      'command': {
        'type': 'Initialize',
        'playerId': 'piotr',
        'payload': {
          'players': ['piotr', 'bot']
        }
      }
    };
    channel.sink.add(jsonEncode(payload));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          Navigator.of(context).push(
            MaterialPageRoute<void>(
              builder: (_) => const ChatPage(),
            ),
          );
        },
        child: const Icon(Icons.chat_bubble_rounded),
      ),
      body: Stack(
        children: <Widget>[
          const Positioned.fill(child: FeltTableBackground()),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                children: <Widget>[
                  _buildTopBar(),
                  const SizedBox(height: 10),
                  _buildPlayersRow(),
                  const SizedBox(height: 12),
                  _buildTrickArea(),
                  const SizedBox(height: 12),
                  _buildActionButtons(),
                  const Spacer(),
                  _buildCreateGameBox(),
                  const SizedBox(height: 12),
                  _buildHandRow(),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTopBar() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: _panelDecoration(),
      child: const Row(
        children: <Widget>[
          Icon(Icons.arrow_back_ios_new, color: Colors.white),
          SizedBox(width: 8),
          Expanded(
            child: Text(
              'Round: 5  •  Turn: Piotr',
              style: TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w700,
                fontSize: 22,
              ),
              textAlign: TextAlign.center,
            ),
          ),
          Icon(Icons.emoji_events_rounded, color: Color(0xFFF0D773)),
        ],
      ),
    );
  }

  Widget _buildPlayersRow() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: _panelDecoration(),
      child: const Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: <Widget>[
          _PlayerTile(name: 'Greta', score: '180'),
          _PlayerTile(name: 'Piotr', score: '215'),
        ],
      ),
    );
  }

  Widget _buildTrickArea() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: _panelDecoration(),
      child: Column(
        children: <Widget>[
          const Text(
            'Trick Area',
            style: TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.w800,
              fontSize: 34,
            ),
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: const <Widget>[
              _CardFace(label: '6 ♠'),
              _CardFace(label: '7 ♠'),
              _CardFace(label: '8 ♠'),
              _CardFace(label: 'J WILD'),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildActionButtons() {
    return Row(
      children: const <Widget>[
        Expanded(child: _TableButton(label: 'Play', color: Color(0xFF7BB83C))),
        SizedBox(width: 10),
        Expanded(child: _TableButton(label: 'Pass', color: Color(0xFF3B4B55))),
        SizedBox(width: 10),
        Expanded(child: _TableButton(label: 'Clear', color: Color(0xFF3B4B55))),
      ],
    );
  }

  Widget _buildCreateGameBox() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: _panelDecoration(),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          FilledButton.icon(
            onPressed: _createGame,
            icon: const Icon(Icons.add_circle_outline),
            label: const Text('Stworz gre'),
          ),
          const SizedBox(height: 8),
          Text(
            _gameId == null ? _gameStatus : 'GameId: $_gameId\n$_gameStatus',
            style: const TextStyle(color: Colors.white70),
          ),
        ],
      ),
    );
  }

  Widget _buildHandRow() {
    const cards = <String>['K', 'A', '2', '3', '4', '4', 'A', 'A', 'A', '4'];

    return Container(
      padding: const EdgeInsets.all(8),
      decoration: _panelDecoration(),
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: cards
              .map(
                (String c) => Padding(
                  padding: const EdgeInsets.only(right: 6),
                  child: _CardFace(label: c),
                ),
              )
              .toList(),
        ),
      ),
    );
  }

  BoxDecoration _panelDecoration() {
    return BoxDecoration(
      borderRadius: BorderRadius.circular(12),
      color: const Color(0x66203F37),
      border: Border.all(color: const Color(0x88C8B66E), width: 1),
      boxShadow: const <BoxShadow>[
        BoxShadow(color: Color(0x44000000), blurRadius: 10, offset: Offset(0, 3)),
      ],
    );
  }
}

class ChatPage extends StatefulWidget {
  const ChatPage({super.key});

  @override
  State<ChatPage> createState() => _ChatPageState();
}

class _ChatPageState extends State<ChatPage> {
  final TextEditingController _playerController = TextEditingController(text: 'piotr');
  final TextEditingController _messageController = TextEditingController();
  final List<ChatEntry> _messages = <ChatEntry>[];

  WebSocketChannel? _channel;
  StreamSubscription<dynamic>? _subscription;
  String _status = 'Rozlaczony';

  String get _chatApiHost {
    if (kIsWeb) {
      return 'localhost';
    }
    if (defaultTargetPlatform == TargetPlatform.android) {
      return '10.0.2.2';
    }
    return 'localhost';
  }

  @override
  void dispose() {
    _subscription?.cancel();
    _channel?.sink.close();
    _playerController.dispose();
    _messageController.dispose();
    super.dispose();
  }

  void _connect() {
    _subscription?.cancel();
    _channel?.sink.close();

    final uri = Uri.parse('ws://$_chatApiHost:5167/ws/chat/global');
    final channel = WebSocketChannel.connect(uri);
    _channel = channel;

    setState(() {
      _status = 'Laczenie...';
    });

    _subscription = channel.stream.listen(
      (dynamic data) {
        final entry = _parseServerMessage(data.toString());
        setState(() {
          _status = 'Polaczony';
          _messages.add(entry);
        });
      },
      onError: (Object error) {
        setState(() {
          _status = 'Blad: $error';
          _channel = null;
        });
      },
      onDone: () {
        setState(() {
          _status = 'Rozlaczony';
          _channel = null;
        });
      },
    );
  }

  void _disconnect() {
    _subscription?.cancel();
    _channel?.sink.close();
    setState(() {
      _status = 'Rozlaczony';
      _channel = null;
    });
  }

  void _send() {
    final player = _playerController.text.trim();
    final text = _messageController.text.trim();
    if (_channel == null || player.isEmpty || text.isEmpty) {
      return;
    }

    final payload = <String, String>{
      'playerId': player,
      'text': text,
    };
    _channel!.sink.add(jsonEncode(payload));
    _messageController.clear();
  }

  ChatEntry _parseServerMessage(String raw) {
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! Map<String, dynamic>) {
        return ChatEntry(playerId: 'system', text: raw);
      }

      final playerId = (decoded['playerId'] ?? decoded['PlayerId'])?.toString();
      final text = (decoded['text'] ?? decoded['Text'])?.toString();
      final title = (decoded['title'] ?? decoded['Title'])?.toString();
      if (playerId != null && text != null) {
        return ChatEntry(playerId: playerId, text: text);
      }
      if (title != null) {
        return ChatEntry(playerId: 'error', text: title);
      }
      return ChatEntry(playerId: 'system', text: raw);
    } catch (_) {
      return ChatEntry(playerId: 'system', text: raw);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Czat Globalny')),
      body: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          children: <Widget>[
            Row(
              children: <Widget>[
                Expanded(
                  child: TextField(
                    controller: _playerController,
                    decoration: const InputDecoration(
                      labelText: 'Player ID',
                      border: OutlineInputBorder(),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                FilledButton(onPressed: _connect, child: const Text('Polacz')),
                const SizedBox(width: 8),
                OutlinedButton(onPressed: _disconnect, child: const Text('Rozlacz')),
              ],
            ),
            const SizedBox(height: 8),
            Align(
              alignment: Alignment.centerLeft,
              child: Text('Status: $_status'),
            ),
            const SizedBox(height: 8),
            Expanded(
              child: Container(
                width: double.infinity,
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.05),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.white24),
                ),
                child: _messages.isEmpty
                    ? const Center(child: Text('Brak wiadomosci'))
                    : ListView.builder(
                        itemCount: _messages.length,
                        itemBuilder: (BuildContext context, int index) {
                          final item = _messages[index];
                          return ListTile(
                            dense: true,
                            title: Text(item.playerId),
                            subtitle: Text(item.text),
                          );
                        },
                      ),
              ),
            ),
            const SizedBox(height: 8),
            Row(
              children: <Widget>[
                Expanded(
                  child: TextField(
                    controller: _messageController,
                    decoration: const InputDecoration(
                      labelText: 'Wiadomosc',
                      border: OutlineInputBorder(),
                    ),
                    onSubmitted: (_) => _send(),
                  ),
                ),
                const SizedBox(width: 8),
                FilledButton(
                  onPressed: _channel != null ? _send : null,
                  child: const Text('Wyslij'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class FeltTableBackground extends StatelessWidget {
  const FeltTableBackground({super.key});

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: <Widget>[
        Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
              colors: <Color>[
                Color(0xFF2F6B5A),
                Color(0xFF1F4E43),
                Color(0xFF123A32),
              ],
            ),
          ),
        ),
        Positioned.fill(
          child: IgnorePointer(
            child: CustomPaint(painter: _FeltNoisePainter()),
          ),
        ),
      ],
    );
  }
}

class _FeltNoisePainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    const step = 6.0;
    final paint = Paint()..style = PaintingStyle.fill;

    for (double y = 0; y < size.height; y += step) {
      for (double x = 0; x < size.width; x += step) {
        final hash = ((x * 13 + y * 17).toInt()) % 100;
        if (hash < 9) {
          paint.color = Colors.white.withValues(alpha: 0.04);
          canvas.drawCircle(Offset(x, y), 0.8, paint);
        } else if (hash > 95) {
          paint.color = Colors.black.withValues(alpha: 0.05);
          canvas.drawCircle(Offset(x, y), 0.9, paint);
        }
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class _PlayerTile extends StatelessWidget {
  const _PlayerTile({
    required this.name,
    required this.score,
  });

  final String name;
  final String score;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: <Widget>[
        const CircleAvatar(
          radius: 23,
          backgroundColor: Color(0xFF2B7E9C),
          child: Icon(Icons.person, color: Colors.white),
        ),
        const SizedBox(width: 8),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Text(
              name,
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w700,
                fontSize: 20,
              ),
            ),
            Text(
              score,
              style: const TextStyle(
                color: Color(0xFFF0D773),
                fontWeight: FontWeight.w700,
                fontSize: 18,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _CardFace extends StatelessWidget {
  const _CardFace({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 64,
      height: 96,
      decoration: BoxDecoration(
        color: const Color(0xFFF4F1E7),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: const Color(0xFFDDD7C8)),
      ),
      child: Center(
        child: Text(
          label,
          style: const TextStyle(
            fontWeight: FontWeight.w800,
            color: Color(0xFF1B222A),
          ),
        ),
      ),
    );
  }
}

class _TableButton extends StatelessWidget {
  const _TableButton({
    required this.label,
    required this.color,
  });

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(10),
        boxShadow: const <BoxShadow>[
          BoxShadow(color: Color(0x55000000), blurRadius: 8, offset: Offset(0, 2)),
        ],
      ),
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Text(
        label,
        textAlign: TextAlign.center,
        style: const TextStyle(
          color: Colors.white,
          fontWeight: FontWeight.w700,
          fontSize: 30,
        ),
      ),
    );
  }
}

class ChatEntry {
  ChatEntry({
    required this.playerId,
    required this.text,
  });

  final String playerId;
  final String text;
}
