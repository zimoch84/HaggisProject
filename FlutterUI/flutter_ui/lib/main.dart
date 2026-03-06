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
      title: 'Haggis Lobby',
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF2D6A4F),
          brightness: Brightness.dark,
        ),
        scaffoldBackgroundColor: const Color(0xFF0D1B1E),
      ),
      home: const LobbyPage(),
    );
  }
}

class LobbyPage extends StatefulWidget {
  const LobbyPage({super.key});

  @override
  State<LobbyPage> createState() => _LobbyPageState();
}

class _LobbyPageState extends State<LobbyPage> {
  final TextEditingController _playerController = TextEditingController(
    text: 'piotr',
  );
  late final TextEditingController _hostController;
  final List<GameRoomViewModel> _rooms = <GameRoomViewModel>[];

  WebSocketChannel? _lobbyChannel;
  StreamSubscription<dynamic>? _lobbySubscription;
  Completer<GameRoomViewModel?>? _pendingCreate;
  String _status = 'Rozlaczony';
  bool _isLoadingRooms = false;

  String get _host => _hostController.text.trim();

  Uri get _lobbyUri => Uri.parse('ws://$_host:6666/ws/global/chat');

  @override
  void initState() {
    super.initState();
    _hostController = TextEditingController(text: _defaultBackendHost());
  }

  @override
  void dispose() {
    _pendingCreate?.complete(null);
    _lobbySubscription?.cancel();
    _lobbyChannel?.sink.close();
    _playerController.dispose();
    _hostController.dispose();
    super.dispose();
  }

  Future<void> _connectLobby() async {
    if (_host.isEmpty) {
      _showError('Podaj backend host.');
      return;
    }

    await _lobbySubscription?.cancel();
    await _lobbyChannel?.sink.close();

    setState(() {
      _status = 'Laczenie z lobby...';
    });

    try {
      final channel = WebSocketChannel.connect(_lobbyUri);
      _lobbyChannel = channel;

      _lobbySubscription = channel.stream.listen(
        (dynamic data) {
          _handleLobbyMessage(data.toString());
        },
        onError: (Object error) {
          if (!mounted) {
            return;
          }
          setState(() {
            _status = 'Blad lobby: $error';
          });
        },
        onDone: () {
          if (!mounted) {
            return;
          }
          setState(() {
            _status = 'Lobby rozlaczone';
          });
        },
      );

      _requestRooms();
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _status = 'Nie mozna polaczyc z ws://$_host:6666';
      });
    }
  }

  void _handleLobbyMessage(String raw) {
    final json = _tryDecodeMap(raw);
    if (json == null) {
      return;
    }

    final type = _readString(json, const <String>['type', 'Type']);
    final operation = _readString(json, const <String>[
      'operation',
      'Operation',
    ]);

    if (type == 'GlobalChatBootstrap') {
      if (!mounted) {
        return;
      }
      setState(() {
        _status = 'Lobby polaczone';
      });
      _requestRooms();
      return;
    }

    if (operation == 'listroom') {
      final data = _readMap(json, const <String>['data', 'Data']);
      final roomsJson = data?['rooms'];
      final parsedRooms = _parseRooms(roomsJson);
      if (!mounted) {
        return;
      }
      setState(() {
        _rooms
          ..clear()
          ..addAll(parsedRooms);
        _status = 'Lobby polaczone';
        _isLoadingRooms = false;
      });
      return;
    }

    if (operation == 'createroom') {
      final data = _readMap(json, const <String>['data', 'Data']);
      final roomJson = _readMap(data, const <String>['room', 'Room']);
      final room = roomJson == null
          ? null
          : GameRoomViewModel.fromJson(roomJson);
      _pendingCreate?.complete(room);
      _pendingCreate = null;
      _requestRooms();
      return;
    }

    if (type == 'OperationRejected') {
      final error =
          _readString(json, const <String>['error', 'Error']) ??
          'Nieznany blad lobby';
      _pendingCreate?.complete(null);
      _pendingCreate = null;
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(error)));
    }
  }

  void _requestRooms() {
    if (_lobbyChannel == null) {
      return;
    }

    setState(() {
      _isLoadingRooms = true;
    });

    _lobbyChannel!.sink.add(jsonEncode(const GlobalListRoomOperation().toJson()));
  }

  Future<void> _createRoom() async {
    final playerId = _playerController.text.trim();
    if (playerId.isEmpty) {
      _showError('Podaj playerId.');
      return;
    }

    var roomNameValue = '';
    final roomName = await showDialog<String>(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: const Text('Nowa gra'),
          content: TextField(
            onChanged: (String value) {
              roomNameValue = value.trim();
            },
            decoration: const InputDecoration(
              labelText: 'Nazwa pokoju',
              hintText: 'np. Wieczorny Haggis',
            ),
          ),
          actions: <Widget>[
            TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: const Text('Anuluj'),
            ),
            FilledButton(
              onPressed: () => Navigator.of(context).pop(roomNameValue),
              child: const Text('Create'),
            ),
          ],
        );
      },
    );

    if (roomName == null) {
      return;
    }

    final completer = Completer<GameRoomViewModel?>();
    _pendingCreate = completer;

    _lobbyChannel?.sink.add(
      jsonEncode(
        GlobalCreateRoomOperation(
          playerId: playerId,
          gameType: 'haggis',
          roomName: roomName.isNotEmpty ? roomName : null,
        ).toJson(),
      ),
    );

    final room = await completer.future.timeout(
      const Duration(seconds: 5),
      onTimeout: () => null,
    );

    if (!mounted) {
      return;
    }

    if (room == null) {
      _showError('Nie udalo sie utworzyc gry.');
      return;
    }

    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => GamePage(
          playerId: playerId,
          initialRoom: room,
          isHost: true,
          host: _host,
        ),
      ),
    );

    _requestRooms();
  }

  Future<void> _joinRoom(GameRoomViewModel room) async {
    final playerId = _playerController.text.trim();
    if (playerId.isEmpty) {
      _showError('Podaj playerId.');
      return;
    }

    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => GamePage(
          playerId: playerId,
          initialRoom: room,
          isHost: false,
          host: _host,
        ),
      ),
    );

    _requestRooms();
  }

  List<GameRoomViewModel> _parseRooms(dynamic roomsJson) {
    if (roomsJson is! List<dynamic>) {
      return const <GameRoomViewModel>[];
    }

    return roomsJson
        .whereType<Map<String, dynamic>>()
        .map(GameRoomViewModel.fromJson)
        .toList()
      ..sort(
        (GameRoomViewModel a, GameRoomViewModel b) =>
            b.createdAt.compareTo(a.createdAt),
      );
  }

  void _showError(String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }

  String _defaultBackendHost() {
    if (kIsWeb) {
      return 'localhost';
    }
    if (defaultTargetPlatform == TargetPlatform.android) {
      return '10.0.2.2';
    }
    return 'localhost';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: DecoratedBox(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: <Color>[
              Color(0xFF102226),
              Color(0xFF17353B),
              Color(0xFF214C43),
            ],
          ),
        ),
        child: SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                const Text(
                  'Haggis Lobby',
                  style: TextStyle(
                    fontSize: 34,
                    fontWeight: FontWeight.w800,
                    color: Colors.white,
                  ),
                ),
                const SizedBox(height: 6),
                Text(_status, style: const TextStyle(color: Colors.white70)),
                const SizedBox(height: 20),
                _SurfaceCard(
                  child: LayoutBuilder(
                    builder: (BuildContext context, BoxConstraints constraints) {
                      final isCompact = constraints.maxWidth < 520;

                      return Column(
                        children: <Widget>[
                          if (isCompact) ...<Widget>[
                            TextField(
                              controller: _playerController,
                              decoration: const InputDecoration(
                                labelText: 'Player ID',
                                border: OutlineInputBorder(),
                              ),
                            ),
                            const SizedBox(height: 12),
                            TextField(
                              controller: _hostController,
                              decoration: const InputDecoration(
                                labelText: 'Backend host',
                                hintText: 'np. 192.168.0.15',
                                border: OutlineInputBorder(),
                              ),
                            ),
                          ] else
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
                                const SizedBox(width: 12),
                                Expanded(
                                  child: TextField(
                                    controller: _hostController,
                                    decoration: const InputDecoration(
                                      labelText: 'Backend host',
                                      hintText: 'np. 192.168.0.15',
                                      border: OutlineInputBorder(),
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          const SizedBox(height: 12),
                          Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children: <Widget>[
                              FilledButton.icon(
                                onPressed: _connectLobby,
                                icon: const Icon(Icons.link),
                                label: const Text('Connect'),
                              ),
                              FilledButton.icon(
                                onPressed: _createRoom,
                                icon: const Icon(Icons.add),
                                label: const Text('New'),
                              ),
                              OutlinedButton.icon(
                                onPressed: _requestRooms,
                                icon: _isLoadingRooms
                                    ? const SizedBox(
                                        width: 16,
                                        height: 16,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                        ),
                                      )
                                    : const Icon(Icons.refresh),
                                label: const Text('Refresh'),
                              ),
                            ],
                          ),
                        ],
                      );
                    },
                  ),
                ),
                const SizedBox(height: 20),
                const Text(
                  'Lista gier',
                  style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w700,
                    color: Colors.white,
                  ),
                ),
                const SizedBox(height: 12),
                Expanded(
                  child: _rooms.isEmpty
                      ? const _EmptyLobbyState()
                      : ListView.separated(
                          itemCount: _rooms.length,
                          separatorBuilder: (BuildContext context, int index) =>
                              const SizedBox(height: 12),
                          itemBuilder: (BuildContext context, int index) {
                            final room = _rooms[index];
                            return _SurfaceCard(
                              child: Row(
                                children: <Widget>[
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: <Widget>[
                                        Text(
                                          room.roomName,
                                          style: const TextStyle(
                                            fontSize: 18,
                                            fontWeight: FontWeight.w700,
                                          ),
                                        ),
                                        const SizedBox(height: 4),
                                        Text('Game ID: ${room.gameId}'),
                                        const SizedBox(height: 4),
                                        Text(
                                          'Gracze: ${room.players.join(', ')}',
                                        ),
                                      ],
                                    ),
                                  ),
                                  FilledButton(
                                    onPressed: () => _joinRoom(room),
                                    child: const Text('Join'),
                                  ),
                                ],
                              ),
                            );
                          },
                        ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class GamePage extends StatefulWidget {
  const GamePage({
    required this.playerId,
    required this.initialRoom,
    required this.isHost,
    required this.host,
    super.key,
  });

  final String playerId;
  final GameRoomViewModel initialRoom;
  final bool isHost;
  final String host;

  @override
  State<GamePage> createState() => _GamePageState();
}

class _GamePageState extends State<GamePage> {
  WebSocketChannel? _gameChannel;
  StreamSubscription<dynamic>? _gameSubscription;
  late GameRoomViewModel _room;
  GameSnapshotViewModel? _snapshot;
  String _status = 'Laczenie do gry...';
  bool _hasStartedGame = false;
  bool _isSendingMove = false;
  List<String> _selectedCards = <String>[];

  Uri get _gameUri => Uri.parse(
    'ws://${widget.host}:6666${_room.gameEndpoint ?? '/ws/games/${widget.initialRoom.gameId}'}',
  );

  @override
  void initState() {
    super.initState();
    _room = widget.initialRoom;
    unawaited(_connect());
  }

  @override
  void dispose() {
    _gameSubscription?.cancel();
    _gameChannel?.sink.close();
    super.dispose();
  }

  Future<void> _connect() async {
    final channel = WebSocketChannel.connect(_gameUri);
    _gameChannel = channel;

    setState(() {
      _status = 'Dolaczanie do pokoju...';
    });

    _gameSubscription = channel.stream.listen(
      (dynamic data) {
        _handleGameMessage(data.toString());
      },
      onError: (Object error) {
        if (!mounted) {
          return;
        }
        setState(() {
          _status = 'Blad gry: $error';
        });
      },
      onDone: () {
        if (!mounted) {
          return;
        }
        setState(() {
          _status = 'Polaczenie z gra zamkniete';
        });
      },
    );

    _sendJoin();
  }

  void _sendJoin() {
    _gameChannel?.sink.add(
      jsonEncode(GameJoinOperation(playerId: widget.playerId).toJson()),
    );
  }

  void _requestSnapshot() {
    _gameChannel?.sink.add(
      jsonEncode(GameSnapshotOperation(playerId: widget.playerId).toJson()),
    );
  }

  void _startGame() {
    if (_room.players.length < 2) {
      _showError('Do startu potrzeba co najmniej 2 graczy.');
      return;
    }

    _gameChannel?.sink.add(
      jsonEncode(
        GameCreateOperation(
          playerId: widget.playerId,
          payload: HaggisCreateOptionsPayload(
            playerCount: _room.players.length,
            players: _room.players,
          ),
        ).toJson(),
      ),
    );

    setState(() {
      _status = 'Start gry...';
    });
  }

  void _playCard(String cardLabel) {
    if (_gameChannel == null || _snapshot == null || _isSendingMove) {
      return;
    }

    if (_snapshot!.currentPlayerId != widget.playerId) {
      _showError('To nie jest twoja tura.');
      return;
    }

    _toggleSelectedCard(cardLabel);
  }

  void _toggleSelectedCard(String cardLabel) {
    setState(() {
      if (_selectedCards.contains(cardLabel)) {
        _selectedCards.remove(cardLabel);
      } else {
        _selectedCards.add(cardLabel);
      }
    });
  }

  void _addSelectedCard(String cardLabel) {
    if (_snapshot == null || _isSendingMove) {
      return;
    }

    if (!_snapshot!.canPlayCard(widget.playerId, cardLabel)) {
      return;
    }

    setState(() {
      if (!_selectedCards.contains(cardLabel)) {
        _selectedCards.add(cardLabel);
      }
    });
  }

  void _clearSelectedCards() {
    setState(() {
      _selectedCards = <String>[];
    });
  }

  void _confirmSelectedPlay() {
    if (_gameChannel == null || _snapshot == null || _isSendingMove) {
      return;
    }

    if (_snapshot!.currentPlayerId != widget.playerId) {
      _showError('To nie jest twoja tura.');
      return;
    }

    final action = _snapshot!.resolvePlayActionForCards(_selectedCards);
    if (action == null) {
      _showError('Zaznaczone karty nie tworza legalnego ruchu.');
      return;
    }

    _gameChannel!.sink.add(
      jsonEncode(
        GameCommandOperation(
          command: GameCommandRequest(
            type: 'Play',
            playerId: widget.playerId,
            payload: GameCommandRequestPayload(action: action),
          ),
        ).toJson(),
      ),
    );

    setState(() {
      _isSendingMove = true;
      _selectedCards = <String>[];
      _status = 'Wysylanie ruchu...';
    });
  }

  void _sendPass() {
    if (_gameChannel == null || _snapshot == null || _isSendingMove) {
      return;
    }

    if (!_snapshot!.canPass(widget.playerId)) {
      _showError('Pass nie jest teraz legalny.');
      return;
    }

    _gameChannel!.sink.add(
      jsonEncode(
        GameCommandOperation(
          command: GameCommandRequest(
            type: 'Pass',
            playerId: widget.playerId,
          ),
        ).toJson(),
      ),
    );

    setState(() {
      _isSendingMove = true;
      _selectedCards = <String>[];
      _status = 'Pass...';
    });
  }

  void _handleGameMessage(String raw) {
    final json = _tryDecodeMap(raw);
    if (json == null) {
      return;
    }

    final type = _readString(json, const <String>['type', 'Type']);
    if (type == 'RoomJoined') {
      final roomJson = _readMap(json, const <String>['room', 'Room']);
      if (roomJson != null && mounted) {
        setState(() {
          _room = GameRoomViewModel.fromJson(roomJson);
          _status = 'Pokoj gotowy';
        });
      }
      _requestSnapshot();
      return;
    }

    if (type == 'OperationRejected' ||
        type == 'CommandRejected' ||
        type == 'ChatRejected') {
      final error =
          _readString(json, const <String>['error', 'Error']) ??
          'Operacja odrzucona';
      if (!mounted) {
        return;
      }
      setState(() {
        _status = error;
        _isSendingMove = false;
      });
      return;
    }

    if (type == 'CommandApplied' || type == 'GameSnapshot') {
      final snapshot = GameSnapshotViewModel.tryFromEvent(json);
      final orderPointer =
          _readInt(json, const <String>['OrderPointer', 'orderPointer']) ?? 0;
      if (!mounted) {
        return;
      }
      setState(() {
        if (snapshot != null) {
          _snapshot = snapshot;
        }
        if (type == 'CommandApplied' || orderPointer > 0) {
          _hasStartedGame = true;
        }
        _isSendingMove = false;
        _selectedCards = <String>[];
        _status = type == 'GameSnapshot' ? 'Stan gry odswiezony' : 'Gra trwa';
      });
      return;
    }

    if (type == 'CommandRejected') {
      if (!mounted) {
        return;
      }
      setState(() {
        _isSendingMove = false;
      });
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    final ownPlayer = _snapshot?.players.firstWhereOrNull(
      (GamePlayerViewModel player) => player.id == widget.playerId,
    );
    final opponents =
        _snapshot?.players
            .where((GamePlayerViewModel player) => player.id != widget.playerId)
            .toList() ??
        _room.players
            .where((String playerId) => playerId != widget.playerId)
            .map((String playerId) => GamePlayerViewModel.placeholder(playerId))
            .toList();
    final sortedSelectedCards = _sortCardLabels(_selectedCards);
    final canSubmitSelected =
        _snapshot?.resolvePlayActionForCards(sortedSelectedCards) != null &&
        !_isSendingMove;
    final canPass = (_snapshot?.canPass(widget.playerId) ?? false) && !_isSendingMove;
    final trick = _snapshot?.trick ?? const <GameTrickItemViewModel>[];
    final latestTrick = trick.isEmpty ? null : trick.last;

    return Scaffold(
      appBar: AppBar(
        title: Text(_room.roomName),
        actions: <Widget>[
          IconButton(
            onPressed: _requestSnapshot,
            tooltip: 'Odswiez stan',
            icon: const Icon(Icons.sync),
          ),
          Padding(
            padding: const EdgeInsets.only(right: 16),
            child: Center(
              child: Text(_status, style: const TextStyle(fontSize: 12)),
            ),
          ),
        ],
      ),
      body: Stack(
        children: <Widget>[
          const Positioned.fill(child: _GameBackground()),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  if (_hasStartedGame)
                    _GameInfoRibbon(
                      roomName: _room.roomName,
                      playerCount: _room.players.length,
                      roundNumber: _snapshot?.roundNumber,
                      currentPlayerId: _snapshot?.currentPlayerId,
                      opponents: opponents,
                    )
                  else
                    _SurfaceCard(
                      padding: const EdgeInsets.all(12),
                      child: LayoutBuilder(
                        builder: (BuildContext context, BoxConstraints constraints) {
                          final isCompact = constraints.maxWidth < 440;

                          final details = Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: <Widget>[
                              Text(
                                'Room: ${_room.roomName}',
                                style: const TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                'Gracze: ${_room.players.join(', ')}',
                                style: const TextStyle(fontSize: 13),
                              ),
                              if (_snapshot != null) ...<Widget>[
                                const SizedBox(height: 2),
                                Text(
                                  'Runda: ${_snapshot!.roundNumber}',
                                  style: const TextStyle(fontSize: 13),
                                ),
                                Text(
                                  'Tura: ${_snapshot!.currentPlayerId}',
                                  style: const TextStyle(fontSize: 13),
                                ),
                              ],
                            ],
                          );

                          final startButton = !_hasStartedGame
                              ? FilledButton(
                                  onPressed: widget.isHost ? _startGame : null,
                                  child: const Text('Start game'),
                                )
                              : null;

                          if (isCompact) {
                            return Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: <Widget>[
                                details,
                                if (startButton != null) ...<Widget>[
                                  const SizedBox(height: 12),
                                  startButton,
                                ],
                              ],
                            );
                          }

                          return Row(
                            children: <Widget>[
                              Expanded(child: details),
                              ?startButton,
                            ],
                          );
                        },
                      ),
                    ),
                  const SizedBox(height: 10),
                  if ((_snapshot?.currentPlayerId == widget.playerId) ||
                      sortedSelectedCards.isNotEmpty) ...<Widget>[
                    _SurfaceCard(
                      padding: const EdgeInsets.all(12),
                      child: _MoveComposer(
                        onClear: _clearSelectedCards,
                        onSubmit: _confirmSelectedPlay,
                        onPass: _sendPass,
                        canSubmit: canSubmitSelected,
                        canPass: canPass,
                        isActiveTurn:
                            _snapshot?.currentPlayerId == widget.playerId,
                      ),
                    ),
                    const SizedBox(height: 10),
                  ],
                  Expanded(
                    child: _SurfaceCard(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: <Widget>[
                          const Text(
                            'Stol',
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                          const SizedBox(height: 12),
                          Expanded(
                            child: sortedSelectedCards.isNotEmpty
                                ? SingleChildScrollView(
                                    child: _PendingTrickView(
                                      cards: sortedSelectedCards,
                                      onCardTap: _toggleSelectedCard,
                                    ),
                                  )
                                : latestTrick == null
                                ? const Center(
                                    child: Text('Brak zagranych kart'),
                                  )
                                : SingleChildScrollView(
                                    child: _LatestTrickView(item: latestTrick),
                                  ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'Twoja reka',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w700,
                      color: Colors.white,
                    ),
                  ),
                  const SizedBox(height: 8),
                  SizedBox(
                    height: ownPlayer != null && ownPlayer.hand.length >= 14
                        ? 176
                        : 118,
                    child: ownPlayer == null || ownPlayer.hand.isEmpty
                        ? _SurfaceCard(
                            child: Center(
                              child: Text(
                                _snapshot == null
                                    ? 'Czekam na rozpoczecie gry'
                                    : 'Brak kart na rece',
                              ),
                            ),
                          )
                        : _PlayerHandFan(
                            cards: _sortCardLabels(ownPlayer.hand),
                            onCardTap: _playCard,
                            isCardPlayable: (String card) =>
                                _snapshot?.canPlayCard(widget.playerId, card) ?? false,
                            isCardSelected: (String card) =>
                                _selectedCards.contains(card),
                            onCardDragged: _addSelectedCard,
                          ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class GameRoomViewModel {
  GameRoomViewModel({
    required this.roomId,
    required this.gameId,
    required this.roomName,
    required this.players,
    required this.createdAt,
    required this.gameEndpoint,
  });

  factory GameRoomViewModel.fromJson(Map<String, dynamic> json) {
    return GameRoomViewModel(
      roomId: _readString(json, const <String>['roomId', 'RoomId']) ?? '',
      gameId: _readString(json, const <String>['gameId', 'GameId']) ?? '',
      roomName:
          _readString(json, const <String>['roomName', 'RoomName']) ??
          'Bez nazwy',
      players: _readStringList(json, const <String>['players', 'Players']),
      gameEndpoint:
          _readString(json, const <String>['gameEndpoint', 'GameEndpoint']),
      createdAt:
          DateTime.tryParse(
            _readString(json, const <String>['createdAt', 'CreatedAt']) ?? '',
          ) ??
          DateTime.now(),
    );
  }

  final String roomId;
  final String gameId;
  final String roomName;
  final List<String> players;
  final DateTime createdAt;
  final String? gameEndpoint;
}

class GlobalListRoomOperation {
  const GlobalListRoomOperation();

  Map<String, dynamic> toJson() {
    return <String, dynamic>{'operation': 'listroom'};
  }
}

class GlobalCreateRoomOperation {
  const GlobalCreateRoomOperation({
    required this.playerId,
    this.gameType,
    this.roomName,
    this.roomId,
  });

  final String playerId;
  final String? gameType;
  final String? roomName;
  final String? roomId;

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'operation': 'createroom',
      'payload': <String, dynamic>{
        'playerId': playerId,
        if (gameType != null) 'gameType': gameType,
        if (roomName != null) 'roomName': roomName,
        if (roomId != null) 'roomId': roomId,
      },
    };
  }
}

class GameJoinOperation {
  const GameJoinOperation({required this.playerId});

  final String playerId;

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'operation': 'join',
      'payload': <String, dynamic>{'playerId': playerId},
    };
  }
}

class GameSnapshotOperation {
  const GameSnapshotOperation({required this.playerId});

  final String playerId;

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'operation': 'snapshot',
      'payload': <String, dynamic>{'playerId': playerId},
    };
  }
}

class GameCreateOperation {
  const GameCreateOperation({required this.playerId, this.payload});

  final String playerId;
  final HaggisCreateOptionsPayload? payload;

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'operation': 'create',
      'payload': <String, dynamic>{
        'playerId': playerId,
        if (payload != null) 'payload': payload!.toJson(),
      },
    };
  }
}

class GameCommandOperation {
  const GameCommandOperation({required this.command});

  final GameCommandRequest command;

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'operation': 'command',
      'payload': <String, dynamic>{
        'command': command.toJson(),
      },
    };
  }
}

class GameCommandRequest {
  const GameCommandRequest({
    required this.type,
    required this.playerId,
    this.payload,
  });

  final String type;
  final String playerId;
  final GameCommandRequestPayload? payload;

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'type': type,
      'playerId': playerId,
      if (payload != null) 'payload': payload!.toJson(),
    };
  }
}

class GameCommandRequestPayload {
  const GameCommandRequestPayload({this.action});

  final String? action;

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      if (action != null) 'action': action,
    };
  }
}

class HaggisCreateOptionsPayload {
  const HaggisCreateOptionsPayload({
    this.playerCount,
    this.players,
    this.seed,
  });

  final int? playerCount;
  final List<String>? players;
  final int? seed;

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      if (playerCount != null) 'playerCount': playerCount,
      if (players != null) 'players': players,
      if (seed != null) 'seed': seed,
    };
  }
}

class GameSnapshotViewModel {
  GameSnapshotViewModel({
    required this.roundNumber,
    required this.currentPlayerId,
    required this.players,
    required this.trick,
    required this.possibleActions,
  });

  factory GameSnapshotViewModel.fromState(Map<String, dynamic> data) {
    final dynamic playersJson = data['players'];
    final dynamic trickJson = data['trick'];
    final dynamic possibleActionsJson = data['possibleActions'];

    return GameSnapshotViewModel(
      roundNumber: _readInt(data, const <String>['roundNumber']) ?? 1,
      currentPlayerId:
          _readString(data, const <String>['currentPlayerId']) ?? '-',
      players: playersJson is List<dynamic>
          ? playersJson
                .whereType<Map<String, dynamic>>()
                .map(GamePlayerViewModel.fromJson)
                .toList()
          : const <GamePlayerViewModel>[],
      trick: trickJson is List<dynamic>
          ? trickJson
                .whereType<Map<String, dynamic>>()
                .map(GameTrickItemViewModel.fromJson)
                .toList()
          : const <GameTrickItemViewModel>[],
      possibleActions: possibleActionsJson is List<dynamic>
          ? possibleActionsJson
                .whereType<Map<String, dynamic>>()
                .map(GamePossibleActionViewModel.fromJson)
                .toList()
          : const <GamePossibleActionViewModel>[],
    );
  }

  static GameSnapshotViewModel? tryFromEvent(Map<String, dynamic> eventJson) {
    final state = _readMap(eventJson, const <String>['State', 'state']);
    final data = state == null
        ? null
        : _readMap(state, const <String>['Data', 'data']);
    return data == null ? null : GameSnapshotViewModel.fromState(data);
  }

  final int roundNumber;
  final String currentPlayerId;
  final List<GamePlayerViewModel> players;
  final List<GameTrickItemViewModel> trick;
  final List<GamePossibleActionViewModel> possibleActions;

  bool canPlayCard(String playerId, String cardLabel) {
    if (currentPlayerId != playerId) {
      return false;
    }

    return resolvePlayActionForCard(cardLabel) != null;
  }

  bool canPass(String playerId) {
    return currentPlayerId == playerId &&
        possibleActions.any((GamePossibleActionViewModel action) => action.type == 'Pass');
  }

  String? resolvePlayActionForCard(String cardLabel) {
    final normalizedCard = cardLabel.trim().toUpperCase();
    final exactSingle = possibleActions.firstWhereOrNull(
      (GamePossibleActionViewModel action) =>
          action.type == 'Play' &&
          action.action.toUpperCase() == 'SINGLE[$normalizedCard]',
    );
    if (exactSingle != null) {
      return exactSingle.action;
    }

    final firstContaining = possibleActions.firstWhereOrNull(
      (GamePossibleActionViewModel action) =>
          action.type == 'Play' &&
          action.action.toUpperCase().contains('[$normalizedCard]'),
    );
    return firstContaining?.action;
  }

  String? resolvePlayActionForCards(List<String> cardLabels) {
    if (cardLabels.isEmpty) {
      return null;
    }

    final normalizedSelection = _normalizeCardList(cardLabels);
    final matchingAction = possibleActions.firstWhereOrNull((GamePossibleActionViewModel action) {
      if (action.type != 'Play') {
        return false;
      }

      final actionCards = _extractCardsFromAction(action.action);
      return _normalizeCardList(actionCards).join('|') == normalizedSelection.join('|');
    });

    return matchingAction?.action;
  }

  static List<String> _extractCardsFromAction(String action) {
    final start = action.indexOf('[');
    final end = action.lastIndexOf(']');
    if (start < 0 || end <= start) {
      return const <String>[];
    }

    final content = action.substring(start + 1, end);
    return content
        .split(RegExp(r'[|,]'))
        .map((String value) => value.trim().toUpperCase())
        .where((String value) => value.isNotEmpty)
        .toList();
  }

  static List<String> _normalizeCardList(List<String> cardLabels) {
    final normalized = cardLabels
        .map((String value) => value.trim().toUpperCase())
        .where((String value) => value.isNotEmpty)
        .toList();
    normalized.sort(_compareCardLabels);
    return normalized;
  }
}

class GamePossibleActionViewModel {
  GamePossibleActionViewModel({required this.type, required this.action});

  factory GamePossibleActionViewModel.fromJson(Map<String, dynamic> json) {
    return GamePossibleActionViewModel(
      type: _readString(json, const <String>['type']) ?? '',
      action: _readString(json, const <String>['action']) ?? '',
    );
  }

  final String type;
  final String action;
}

class GamePlayerViewModel {
  GamePlayerViewModel({
    required this.id,
    required this.score,
    required this.handCount,
    required this.hand,
    required this.finished,
  });

  factory GamePlayerViewModel.fromJson(Map<String, dynamic> json) {
    return GamePlayerViewModel(
      id: _readString(json, const <String>['id']) ?? 'unknown',
      score: _readInt(json, const <String>['score']) ?? 0,
      handCount: _readInt(json, const <String>['handCount']) ?? 0,
      hand: _readStringList(json, const <String>['hand']),
      finished: _readBool(json, const <String>['finished']) ?? false,
    );
  }

  factory GamePlayerViewModel.placeholder(String id) {
    return GamePlayerViewModel(
      id: id,
      score: 0,
      handCount: 0,
      hand: const <String>[],
      finished: false,
    );
  }

  final String id;
  final int score;
  final int handCount;
  final List<String> hand;
  final bool finished;
}

class GameTrickItemViewModel {
  GameTrickItemViewModel({required this.playerId, required this.description});

  factory GameTrickItemViewModel.fromJson(Map<String, dynamic> json) {
    return GameTrickItemViewModel(
      playerId: _readString(json, const <String>['playerId']) ?? 'unknown',
      description: _readString(json, const <String>['desc', 'action']) ?? '-',
    );
  }

  final String playerId;
  final String description;

  bool get isPass => description.trim().toUpperCase() == 'PASS';

  String get trickType {
    final trimmed = description.trim();
    final bracketIndex = trimmed.indexOf('[');
    if (bracketIndex <= 0) {
      return trimmed;
    }
    return trimmed.substring(0, bracketIndex);
  }

  List<String> get cards {
    final start = description.indexOf('[');
    final end = description.lastIndexOf(']');
    if (start < 0 || end <= start) {
      return const <String>[];
    }

    return description
        .substring(start + 1, end)
        .split(RegExp(r'[|,]'))
        .map((String value) => value.trim().toUpperCase())
        .where((String value) => value.isNotEmpty)
        .toList(growable: false);
  }
}

class _LatestTrickView extends StatelessWidget {
  const _LatestTrickView({required this.item});

  final GameTrickItemViewModel item;

  @override
  Widget build(BuildContext context) {
    final cards = item.cards;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        if (cards.isEmpty)
          Text(
            item.description,
            style: const TextStyle(fontWeight: FontWeight.w700),
          )
        else
          Wrap(
            spacing: 12,
            runSpacing: 12,
            children: cards
                .map((String card) => _TableCardChip(label: card))
                .toList(),
          ),
      ],
    );
  }
}

class _PendingTrickView extends StatelessWidget {
  const _PendingTrickView({
    required this.cards,
    required this.onCardTap,
  });

  final List<String> cards;
  final ValueChanged<String> onCardTap;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 12,
      runSpacing: 12,
      children: cards
          .map(
            (String card) => GestureDetector(
              onTap: () => onCardTap(card),
              child: Stack(
                clipBehavior: Clip.none,
                children: <Widget>[
                  _TableCardChip(label: card),
                  const Positioned(
                    top: -6,
                    right: -6,
                    child: _PendingRemoveBadge(),
                  ),
                ],
              ),
            ),
          )
          .toList(),
    );
  }
}

class _PendingRemoveBadge extends StatelessWidget {
  const _PendingRemoveBadge();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 22,
      height: 22,
      decoration: BoxDecoration(
        color: const Color(0xFFF2C14E),
        shape: BoxShape.circle,
        border: Border.all(color: const Color(0xFFF8F4EC), width: 2),
      ),
      alignment: Alignment.center,
      child: const Icon(
        Icons.close,
        size: 12,
        color: Color(0xFF243238),
      ),
    );
  }
}

class _TableCardChip extends StatelessWidget {
  const _TableCardChip({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    final parsed = _DisplayCardLabel.fromLabel(label);
    final oldStyle = _OldStyleCardAssetSet.fromSuit(parsed.suitToken);

    return SizedBox(
      width: 96,
      height: 136,
      child: Container(
        decoration: BoxDecoration(
          color: const Color(0xFFF8F4EC),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: const Color(0xFFD8CFBD), width: 1.5),
          boxShadow: const <BoxShadow>[
            BoxShadow(
              color: Color(0x22000000),
              blurRadius: 6,
              offset: Offset(0, 3),
            ),
          ],
        ),
        child: Stack(
          children: <Widget>[
            Positioned(
              top: 10,
              left: 10,
              child: _OldStyleCardCorner(
                rank: parsed.rankToken,
                rankColor: oldStyle.rankColor,
                symbolAssetPath: oldStyle.symbolAssetPath,
              ),
            ),
            Positioned(
              bottom: 10,
              right: 10,
              child: Transform.rotate(
                angle: 3.14159,
                child: _OldStyleCardCorner(
                  rank: parsed.rankToken,
                  rankColor: oldStyle.rankColor,
                  symbolAssetPath: oldStyle.symbolAssetPath,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _OldStyleCardCorner extends StatelessWidget {
  const _OldStyleCardCorner({
    required this.rank,
    required this.rankColor,
    required this.symbolAssetPath,
  });

  final String rank;
  final Color rankColor;
  final String symbolAssetPath;

  @override
  Widget build(BuildContext context) {
    return _CardRankAndSymbol(
      rank: rank,
      rankColor: rankColor,
      symbolAssetPath: symbolAssetPath,
      rankSize: 18,
      symbolSize: 22,
    );
  }
}

class _CardRankAndSymbol extends StatelessWidget {
  const _CardRankAndSymbol({
    required this.rank,
    required this.rankColor,
    required this.symbolAssetPath,
    required this.rankSize,
    required this.symbolSize,
  });

  final String rank;
  final Color rankColor;
  final String symbolAssetPath;
  final double rankSize;
  final double symbolSize;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Text(
          rank,
          style: TextStyle(
            color: rankColor,
            fontWeight: FontWeight.w900,
            fontSize: rankSize,
            height: 1,
          ),
        ),
        SizedBox(height: symbolSize <= 16 ? 3 : 4),
        Image.asset(
          symbolAssetPath,
          width: symbolSize,
          height: symbolSize,
          fit: BoxFit.contain,
        ),
      ],
    );
  }
}

class _OldStyleCardAssetSet {
  const _OldStyleCardAssetSet({
    required this.symbolAssetPath,
    required this.rankColor,
  });

  final String symbolAssetPath;
  final Color rankColor;

  static const String _basePath = 'assets/cards/old_style';
  static const String _symbolsPath = '$_basePath/symbols';

  factory _OldStyleCardAssetSet.fromSuit(String suitToken) {
    switch (suitToken) {
      case 'B':
        return const _OldStyleCardAssetSet(
          symbolAssetPath: '$_symbolsPath/black_symbol.png',
          rankColor: Color(0xFF242424),
        );
      case 'G':
        return const _OldStyleCardAssetSet(
          symbolAssetPath: '$_symbolsPath/green_symbol.png',
          rankColor: Color(0xFF246B3A),
        );
      case 'R':
        return const _OldStyleCardAssetSet(
          symbolAssetPath: '$_symbolsPath/red_symbol.png',
          rankColor: Color(0xFFB33A34),
        );
      case 'O':
        return const _OldStyleCardAssetSet(
          symbolAssetPath: '$_symbolsPath/orange_symbol.png',
          rankColor: Color(0xFFB76A16),
        );
      case 'Y':
        return const _OldStyleCardAssetSet(
          symbolAssetPath: '$_symbolsPath/yellow_symbol.png',
          rankColor: Color(0xFF9A6B00),
        );
      default:
        return const _OldStyleCardAssetSet(
          symbolAssetPath: '$_symbolsPath/black_symbol.png',
          rankColor: Color(0xFF242424),
        );
    }
  }
}

class _SurfaceCard extends StatelessWidget {
  const _SurfaceCard({
    required this.child,
    this.padding = const EdgeInsets.all(16),
  });

  final Widget child;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: padding,
      decoration: BoxDecoration(
        color: const Color(0xCC162A2E),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: const Color(0x6656B891)),
        boxShadow: const <BoxShadow>[
          BoxShadow(
            color: Color(0x55000000),
            blurRadius: 20,
            offset: Offset(0, 10),
          ),
        ],
      ),
      child: child,
    );
  }
}

class _GameInfoRibbon extends StatelessWidget {
  const _GameInfoRibbon({
    required this.roomName,
    required this.playerCount,
    required this.roundNumber,
    required this.currentPlayerId,
    required this.opponents,
  });

  final String roomName;
  final int playerCount;
  final int? roundNumber;
  final String? currentPlayerId;
  final List<GamePlayerViewModel> opponents;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: <Widget>[
        _InfoChip(icon: Icons.meeting_room_outlined, label: roomName),
        _InfoChip(icon: Icons.group_outlined, label: '$playerCount'),
        if (roundNumber != null)
          _InfoChip(icon: Icons.casino_outlined, label: 'R$roundNumber'),
        if (currentPlayerId != null && currentPlayerId!.isNotEmpty)
          _InfoChip(icon: Icons.play_circle_outline, label: currentPlayerId!),
        ...opponents.map(
          (GamePlayerViewModel player) => _OpponentPill(player: player),
        ),
      ],
    );
  }
}

class _InfoChip extends StatelessWidget {
  const _InfoChip({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
      decoration: BoxDecoration(
        color: const Color(0xCC162A2E),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: const Color(0x6656B891)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Icon(icon, size: 16, color: const Color(0xFF9BE2BF)),
          const SizedBox(width: 6),
          Text(
            label,
            style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700),
          ),
        ],
      ),
    );
  }
}

class _EmptyLobbyState extends StatelessWidget {
  const _EmptyLobbyState();

  @override
  Widget build(BuildContext context) {
    return const _SurfaceCard(
      child: Center(
        child: Text(
          'Brak aktywnych gier.\nUtworz nowa i dolacz z drugiego klienta.',
          textAlign: TextAlign.center,
        ),
      ),
    );
  }
}

class _OpponentPill extends StatelessWidget {
  const _OpponentPill({required this.player});

  final GamePlayerViewModel player;

  @override
  Widget build(BuildContext context) {
    final initial =
        player.id.isEmpty ? '?' : player.id.characters.first.toUpperCase();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
      decoration: BoxDecoration(
        color: const Color(0xCC162A2E),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: const Color(0x6656B891)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Container(
            width: 28,
            height: 28,
            decoration: const BoxDecoration(
              color: Color(0xFF2D6A4F),
              shape: BoxShape.circle,
            ),
            alignment: Alignment.center,
            child: Text(
              initial,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w900,
                color: Colors.white,
              ),
            ),
          ),
          const SizedBox(width: 8),
          Text(
            player.id,
            style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              color: Colors.white,
            ),
          ),
          const SizedBox(width: 8),
          Icon(
            Icons.style_outlined,
            size: 14,
            color: player.finished
                ? const Color(0x88B9D8C6)
                : const Color(0xFFB9D8C6),
          ),
          const SizedBox(width: 4),
          Text(
            '${player.handCount}',
            style: const TextStyle(fontSize: 12, color: Colors.white70),
          ),
        ],
      ),
    );
  }
}

class _HandCard extends StatelessWidget {
  const _HandCard({
    required this.label,
    required this.isPlayable,
    required this.isSelected,
    this.onTap,
    this.width = 82,
    this.height = 112,
  });

  final String label;
  final bool isPlayable;
  final bool isSelected;
  final VoidCallback? onTap;
  final double width;
  final double height;

  @override
  Widget build(BuildContext context) {
    final parsed = _DisplayCardLabel.fromLabel(label);
    final oldStyle = _OldStyleCardAssetSet.fromSuit(parsed.suitToken);

    return GestureDetector(
      onTap: isPlayable ? onTap : null,
      child: Opacity(
        opacity: isPlayable ? 1 : 0.72,
        child: Container(
          width: width,
          height: height,
          decoration: BoxDecoration(
            color: const Color(0xFFF6F1E7),
            borderRadius: BorderRadius.circular(16),
            border: Border.all(
              color: isSelected
                  ? const Color(0xFFF2C14E)
                  : isPlayable
                  ? const Color(0xFF5AAE7A)
                  : const Color(0xFFD8CFBD),
              width: isSelected || isPlayable ? 2 : 1,
            ),
            boxShadow: const <BoxShadow>[
              BoxShadow(
                color: Color(0x22000000),
                blurRadius: 6,
                offset: Offset(0, 3),
              ),
            ],
          ),
          child: Padding(
            padding: const EdgeInsets.all(8),
            child: Align(
              alignment: Alignment.topLeft,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                decoration: BoxDecoration(
                  color: const Color(0xFFF0E8D8),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: const Color(0x22B4A893)),
                ),
                child: _CardRankAndSymbol(
                  rank: parsed.rankToken,
                  rankColor: oldStyle.rankColor,
                  symbolAssetPath: oldStyle.symbolAssetPath,
                  rankSize: 16,
                  symbolSize: 14,
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _PlayerHandFan extends StatelessWidget {
  const _PlayerHandFan({
    required this.cards,
    required this.onCardTap,
    required this.isCardPlayable,
    required this.isCardSelected,
    required this.onCardDragged,
  });

  final List<String> cards;
  final ValueChanged<String> onCardTap;
  final bool Function(String card) isCardPlayable;
  final bool Function(String card) isCardSelected;
  final ValueChanged<String> onCardDragged;
  static const int _twoRowThreshold = 14;

  @override
  Widget build(BuildContext context) {
    if (cards.length >= _twoRowThreshold) {
      return _TwoRowPlayerHand(
        cards: cards,
        onCardTap: onCardTap,
        isCardPlayable: isCardPlayable,
        isCardSelected: isCardSelected,
        onCardDragged: onCardDragged,
      );
    }

    const cardWidth = 62.0;
    const cardHeight = 96.0;
    const preferredStep = 32.0;
    const minimumStep = 22.0;

    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        final availableWidth = constraints.maxWidth.isFinite
            ? constraints.maxWidth
            : cardWidth;
        final fittedStep = cards.length <= 1
            ? cardWidth
            : (availableWidth - cardWidth) / (cards.length - 1);
        final effectiveStep = cards.length <= 1
            ? cardWidth
            : fittedStep.clamp(minimumStep, preferredStep);
        final contentWidth = cards.isEmpty
            ? cardWidth
            : cardWidth + (cards.length - 1) * effectiveStep;

        return SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: SizedBox(
            width: contentWidth,
            height: 118,
            child: Stack(
              clipBehavior: Clip.none,
              children: <Widget>[
                for (int index = 0; index < cards.length; index++)
                  _buildPositionedCard(
                    index: index,
                    count: cards.length,
                    step: effectiveStep,
                    width: cardWidth,
                    height: cardHeight,
                    label: cards[index],
                    isPlayable: isCardPlayable(cards[index]),
                    isSelected: isCardSelected(cards[index]),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildPositionedCard({
    required int index,
    required int count,
    required double step,
    required double width,
    required double height,
    required String label,
    required bool isPlayable,
    required bool isSelected,
  }) {
    final center = (count - 1) / 2;
    final distanceFromCenter = index - center;
    final normalized = center == 0 ? 0.0 : distanceFromCenter / center;
    final angle = normalized * 0.12;
    final top = 8 + normalized.abs() * 10;

    final rotatedCard = Transform.rotate(
      angle: angle,
      alignment: Alignment.bottomCenter,
      child: _HandCard(
        label: label,
        width: width,
        height: height,
        isPlayable: isPlayable,
        isSelected: isSelected,
        onTap: () => onCardTap(label),
      ),
    );

    return Positioned(
      left: index * step,
      top: top,
      child: isPlayable
          ? Draggable<String>(
              data: label,
              feedback: Material(
                color: Colors.transparent,
                child: _HandCard(
                  label: label,
                  width: width,
                  height: height,
                  isPlayable: true,
                  isSelected: true,
                ),
              ),
              childWhenDragging: Opacity(opacity: 0.35, child: rotatedCard),
              onDragCompleted: () => onCardDragged(label),
              child: rotatedCard,
            )
          : rotatedCard,
    );
  }
}

class _TwoRowPlayerHand extends StatelessWidget {
  const _TwoRowPlayerHand({
    required this.cards,
    required this.onCardTap,
    required this.isCardPlayable,
    required this.isCardSelected,
    required this.onCardDragged,
  });

  final List<String> cards;
  final ValueChanged<String> onCardTap;
  final bool Function(String card) isCardPlayable;
  final bool Function(String card) isCardSelected;
  final ValueChanged<String> onCardDragged;

  @override
  Widget build(BuildContext context) {
    final splitIndex = (cards.length / 2).ceil();
    final topRow = cards.take(splitIndex).toList(growable: false);
    final bottomRow = cards.skip(splitIndex).toList(growable: false);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        SizedBox(
          height: 82,
          child: _PlayerHandFan(
            cards: topRow,
            onCardTap: onCardTap,
            isCardPlayable: isCardPlayable,
            isCardSelected: isCardSelected,
            onCardDragged: onCardDragged,
          ),
        ),
        const SizedBox(height: 10),
        SizedBox(
          height: 82,
          child: _PlayerHandFan(
            cards: bottomRow,
            onCardTap: onCardTap,
            isCardPlayable: isCardPlayable,
            isCardSelected: isCardSelected,
            onCardDragged: onCardDragged,
          ),
        ),
      ],
    );
  }
}

class _MoveComposer extends StatelessWidget {
  const _MoveComposer({
    required this.onClear,
    required this.onSubmit,
    required this.onPass,
    required this.canSubmit,
    required this.canPass,
    required this.isActiveTurn,
  });

  final VoidCallback onClear;
  final VoidCallback onSubmit;
  final VoidCallback onPass;
  final bool canSubmit;
  final bool canPass;
  final bool isActiveTurn;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: <Widget>[
        FilledButton.icon(
          onPressed: canSubmit ? onSubmit : null,
          icon: const Icon(Icons.play_arrow),
          label: const Text('Play selected'),
        ),
        OutlinedButton.icon(
          onPressed: canPass ? onPass : null,
          icon: const Icon(Icons.skip_next),
          label: const Text('Pass'),
        ),
        if (canSubmit)
          TextButton.icon(
            onPressed: onClear,
            icon: const Icon(Icons.clear),
            label: const Text('Clear'),
          ),
      ],
    );
  }
}

class _GameBackground extends StatelessWidget {
  const _GameBackground();

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: <Widget>[
        Container(
          decoration: const BoxDecoration(
            gradient: RadialGradient(
              center: Alignment.topCenter,
              radius: 1.5,
              colors: <Color>[
                Color(0xFF2D6A4F),
                Color(0xFF1E4A42),
                Color(0xFF102428),
              ],
            ),
          ),
        ),
        Positioned.fill(
          child: IgnorePointer(child: CustomPaint(painter: _NoisePainter())),
        ),
      ],
    );
  }
}

class _NoisePainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint();
    const step = 7.0;

    for (double y = 0; y < size.height; y += step) {
      for (double x = 0; x < size.width; x += step) {
        final int hash = ((x * 31 + y * 17).round()) % 100;
        if (hash < 6) {
          paint.color = Colors.white.withValues(alpha: 0.03);
          canvas.drawCircle(Offset(x, y), 1.0, paint);
        }
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

Map<String, dynamic>? _tryDecodeMap(String raw) {
  final dynamic decoded = jsonDecode(raw);
  return decoded is Map<String, dynamic> ? decoded : null;
}

Map<String, dynamic>? _readMap(Map<String, dynamic>? json, List<String> keys) {
  if (json == null) {
    return null;
  }

  for (final String key in keys) {
    final dynamic value = json[key];
    if (value is Map<String, dynamic>) {
      return value;
    }
  }

  return null;
}

String? _readString(Map<String, dynamic> json, List<String> keys) {
  for (final String key in keys) {
    final dynamic value = json[key];
    if (value is String) {
      return value;
    }
  }

  return null;
}

int? _readInt(Map<String, dynamic> json, List<String> keys) {
  for (final String key in keys) {
    final dynamic value = json[key];
    if (value is int) {
      return value;
    }
  }

  return null;
}

bool? _readBool(Map<String, dynamic> json, List<String> keys) {
  for (final String key in keys) {
    final dynamic value = json[key];
    if (value is bool) {
      return value;
    }
  }

  return null;
}

List<String> _readStringList(Map<String, dynamic> json, List<String> keys) {
  for (final String key in keys) {
    final dynamic value = json[key];
    if (value is List<dynamic>) {
      return value.map((dynamic item) => item.toString()).toList();
    }
  }

  return const <String>[];
}

List<String> _sortCardLabels(List<String> cards) {
  final sorted = List<String>.from(cards);
  sorted.sort(_compareCardLabels);
  return sorted;
}

int _compareCardLabels(String left, String right) {
  final leftCard = _ParsedCardLabel.fromRaw(left);
  final rightCard = _ParsedCardLabel.fromRaw(right);

  final rankComparison = leftCard.rankOrder.compareTo(rightCard.rankOrder);
  if (rankComparison != 0) {
    return rankComparison;
  }

  final suitComparison = leftCard.suitOrder.compareTo(rightCard.suitOrder);
  if (suitComparison != 0) {
    return suitComparison;
  }

  return leftCard.raw.compareTo(rightCard.raw);
}

class _ParsedCardLabel {
  const _ParsedCardLabel({
    required this.raw,
    required this.rankOrder,
    required this.suitOrder,
  });

  final String raw;
  final int rankOrder;
  final int suitOrder;

  factory _ParsedCardLabel.fromRaw(String rawLabel) {
    final raw = rawLabel.trim().toUpperCase();
    if (raw.isEmpty) {
      return const _ParsedCardLabel(raw: '', rankOrder: 999, suitOrder: 999);
    }

    final hasSuit = raw.length > 1;
    final rankToken = hasSuit ? raw.substring(0, raw.length - 1) : raw;
    final suitToken = hasSuit ? raw.substring(raw.length - 1) : '';

    return _ParsedCardLabel(
      raw: raw,
      rankOrder: _rankOrder(rankToken),
      suitOrder: _suitOrder(suitToken),
    );
  }

  static int _rankOrder(String rankToken) {
    const namedRanks = <String, int>{
      '2': 2,
      '3': 3,
      '4': 4,
      '5': 5,
      '6': 6,
      '7': 7,
      '8': 8,
      '9': 9,
      '10': 10,
      'J': 11,
      'Q': 12,
      'K': 13,
      'A': 14,
    };

    return namedRanks[rankToken] ?? 999;
  }

  static int _suitOrder(String suitToken) {
    const suits = <String, int>{
      'B': 0,
      'G': 1,
      'R': 2,
      'O': 3,
      'Y': 4,
    };

    return suits[suitToken] ?? 999;
  }
}

class _DisplayCardLabel {
  const _DisplayCardLabel({
    required this.rankToken,
    required this.suitToken,
  });

  final String rankToken;
  final String suitToken;

  factory _DisplayCardLabel.fromLabel(String rawLabel) {
    final raw = rawLabel.trim().toUpperCase();
    if (raw.isEmpty) {
      return const _DisplayCardLabel(rankToken: '?', suitToken: '');
    }

    if (raw.length == 1) {
      return _DisplayCardLabel(rankToken: raw, suitToken: '');
    }

    return _DisplayCardLabel(
      rankToken: raw.substring(0, raw.length - 1),
      suitToken: raw.substring(raw.length - 1),
    );
  }
}

extension _FirstWhereOrNullExtension<T> on Iterable<T> {
  T? firstWhereOrNull(bool Function(T item) test) {
    for (final T item in this) {
      if (test(item)) {
        return item;
      }
    }
    return null;
  }
}
