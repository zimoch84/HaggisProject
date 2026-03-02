import 'package:flutter/material.dart';

import '../commands/command_catalog.dart';
import '../commands/haggis_command.dart';
import '../models/command_input.dart';
import '../services/haggis_socket_client.dart';
import '../services/ws_endpoint_resolver.dart';

class CommandConsolePage extends StatefulWidget {
  const CommandConsolePage({super.key});

  @override
  State<CommandConsolePage> createState() => _CommandConsolePageState();
}

class _CommandConsolePageState extends State<CommandConsolePage> {
  final _hostController = TextEditingController(text: 'localhost');
  final _portController = TextEditingController(text: '8080');
  final _playerIdController = TextEditingController(text: 'piotr');
  final _targetPlayerIdController = TextEditingController(text: 'anna');
  final _textController = TextEditingController(text: 'test z Fluttera');
  final _roomNameController = TextEditingController(text: 'Room Flutter');
  final _roomIdController = TextEditingController(text: 'room-1');
  final _gameIdController = TextEditingController(text: 'game-1');
  final _commandTypeController = TextEditingController(text: 'Play');

  final _resolver = const WsEndpointResolver();
  final List<String> _logs = <String>[];
  HaggisSocketClient? _client;

  @override
  void dispose() {
    _hostController.dispose();
    _portController.dispose();
    _playerIdController.dispose();
    _targetPlayerIdController.dispose();
    _textController.dispose();
    _roomNameController.dispose();
    _roomIdController.dispose();
    _gameIdController.dispose();
    _commandTypeController.dispose();
    _client?.dispose();
    super.dispose();
  }

  Future<void> _runCommand(HaggisCommand command) async {
    final host = _resolver.hostForCurrentPlatform(_hostController.text.trim());
    final port = _portController.text.trim();

    if (host.isEmpty || port.isEmpty) {
      _addLog('Brakuje hosta lub portu.');
      return;
    }

    final input = CommandInput(
      playerId: _playerIdController.text.trim(),
      text: _textController.text.trim(),
      targetPlayerId: _targetPlayerIdController.text.trim(),
      roomName: _roomNameController.text.trim(),
      roomId: _roomIdController.text.trim(),
      gameId: _gameIdController.text.trim(),
      commandType: _commandTypeController.text.trim(),
    );

    if (input.playerId.isEmpty || input.gameId.isEmpty) {
      _addLog('Pola playerId i gameId nie moga byc puste.');
      return;
    }

    _client ??= HaggisSocketClient(
      host: host,
      port: port,
      onMessage: (String path, String message) {
        if (!mounted) {
          return;
        }
        _addLog('[$path] <= $message');
      },
      onError: (String path, Object error) {
        if (!mounted) {
          return;
        }
        _addLog('[$path] ERROR: $error');
      },
    );

    try {
      await command.execute(_client!, input);
      _addLog('[${command.label}] => wyslano');
    } catch (error) {
      _addLog('[${command.label}] BLAD: $error');
    }
  }

  void _addLog(String message) {
    setState(() {
      _logs.insert(0, '${DateTime.now().toIso8601String()}  $message');
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Haggis AsyncAPI Command Sender'),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            children: <Widget>[
              Expanded(
                child: ListView(
                  children: <Widget>[
                    _buildTextField(_hostController, 'Host'),
                    _buildTextField(_portController, 'Port'),
                    _buildTextField(_playerIdController, 'Player ID'),
                    _buildTextField(_targetPlayerIdController, 'Target Player ID'),
                    _buildTextField(_gameIdController, 'Game ID'),
                    _buildTextField(_roomIdController, 'Room ID'),
                    _buildTextField(_roomNameController, 'Room Name'),
                    _buildTextField(_textController, 'Text / Note'),
                    _buildTextField(_commandTypeController, 'Game Command Type'),
                    const SizedBox(height: 8),
                    const Text(
                      'Komendy',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 8),
                    ...CommandCatalog.all.map(_buildCommandButton),
                    const SizedBox(height: 16),
                    const Text(
                      'Log',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 8),
                    Container(
                      constraints: const BoxConstraints(minHeight: 220),
                      decoration: BoxDecoration(
                        border: Border.all(color: Colors.black12),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: _logs.isEmpty
                          ? const Center(
                              child: Padding(
                                padding: EdgeInsets.all(24),
                                child: Text('Brak komunikatow.'),
                              ),
                            )
                          : ListView.builder(
                              shrinkWrap: true,
                              physics: const NeverScrollableScrollPhysics(),
                              itemCount: _logs.length,
                              itemBuilder: (BuildContext context, int index) {
                                return Padding(
                                  padding: const EdgeInsets.all(8),
                                  child: Text(
                                    _logs[index],
                                    style: const TextStyle(fontSize: 12),
                                  ),
                                );
                              },
                            ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton(
                  onPressed: () {
                    setState(_logs.clear);
                  },
                  child: const Text('Wyczysc log'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildTextField(TextEditingController controller, String label) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: TextField(
        controller: controller,
        decoration: InputDecoration(
          labelText: label,
          border: const OutlineInputBorder(),
        ),
      ),
    );
  }

  Widget _buildCommandButton(HaggisCommand command) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: SizedBox(
        width: double.infinity,
        child: FilledButton(
          onPressed: () => _runCommand(command),
          child: Text(command.label),
        ),
      ),
    );
  }
}
