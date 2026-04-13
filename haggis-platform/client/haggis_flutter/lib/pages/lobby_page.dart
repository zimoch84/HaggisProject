import 'package:flutter/material.dart';

import '../controllers/lobby_controller.dart';
import '../models/lobby_models.dart';

class LobbyPage extends StatefulWidget {
  const LobbyPage({
    super.key,
    required this.controller,
    required this.onJoinRoom,
    required this.onDisconnect,
  });

  final LobbyController controller;
  final Future<void> Function(LobbyRoom room) onJoinRoom;
  final VoidCallback onDisconnect;

  @override
  State<LobbyPage> createState() => _LobbyPageState();
}

class _LobbyPageState extends State<LobbyPage> {
  final TextEditingController _roomController = TextEditingController();
  bool _joining = false;

  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_refresh);
  }

  @override
  void dispose() {
    widget.controller.removeListener(_refresh);
    _roomController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final viewModel = widget.controller.viewModel;
    final rooms = viewModel.rooms;

    return Scaffold(
      appBar: AppBar(
        title: Text('Lobby: ${viewModel.playerId}'),
        actions: [
          IconButton(
            onPressed: viewModel.isBusy ? null : widget.controller.refreshRooms,
            icon: const Icon(Icons.refresh),
          ),
          IconButton(
            onPressed: widget.onDisconnect,
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: LayoutBuilder(
        builder: (context, constraints) {
          final isNarrow = constraints.maxWidth < 900;

          if (isNarrow) {
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _buildRoomsCard(context, rooms, fillHeight: false),
                const SizedBox(height: 16),
                _buildGlobalChatCard(context, fillHeight: false),
              ],
            );
          }

          return Row(
            children: [
              Expanded(
                flex: 3,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: _buildRoomsCard(context, rooms, fillHeight: true),
                ),
              ),
              Expanded(
                flex: 2,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(0, 16, 16, 16),
                  child: _buildGlobalChatCard(context, fillHeight: true),
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildRoomsCard(
    BuildContext context,
    List<LobbyRoom> rooms, {
    required bool fillHeight,
  }) {
    final content = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: fillHeight ? MainAxisSize.max : MainAxisSize.min,
      children: [
        Text(
          'Rooms',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: 8),
        Text(widget.controller.viewModel.status),
        const SizedBox(height: 16),
        LayoutBuilder(
          builder: (context, constraints) {
            final stacked = constraints.maxWidth < 420;
            if (stacked) {
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  TextField(
                    controller: _roomController,
                    decoration: const InputDecoration(
                      labelText: 'New room name',
                    ),
                  ),
                  const SizedBox(height: 12),
                  FilledButton(
                    onPressed: widget.controller.viewModel.isBusy ? null : _handleCreate,
                    child: const Text('Create'),
                  ),
                ],
              );
            }

            return Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _roomController,
                    decoration: const InputDecoration(
                      labelText: 'New room name',
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                FilledButton(
                  onPressed: widget.controller.viewModel.isBusy ? null : _handleCreate,
                  child: const Text('Create'),
                ),
              ],
            );
          },
        ),
        const SizedBox(height: 16),
        if (fillHeight)
          Expanded(child: _buildRoomsList(rooms))
        else
          SizedBox(height: 320, child: _buildRoomsList(rooms)),
      ],
    );

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: content,
      ),
    );
  }

  Widget _buildRoomsList(List<LobbyRoom> rooms) {
    return rooms.isEmpty
        ? const Center(child: Text('No rooms yet.'))
        : ListView.separated(
            itemCount: rooms.length,
            separatorBuilder: (context, index) => const Divider(),
            itemBuilder: (context, index) {
              final room = rooms[index];
              return ListTile(
                title: Text(room.roomName),
                subtitle: Text('${room.players.join(', ')}\n${room.gameId}'),
                isThreeLine: true,
                trailing: FilledButton.tonal(
                  onPressed: _joining ? null : () => _joinRoom(room),
                  child: const Text('Join'),
                ),
              );
            },
          );
  }

  Widget _buildGlobalChatCard(
    BuildContext context, {
    required bool fillHeight,
  }) {
    final list = ListView.builder(
      itemCount: widget.controller.messages.length,
      itemBuilder: (context, index) {
        final message = widget.controller.viewModel.messages[index];
        return ListTile(
          dense: true,
          title: Text(message.playerId),
          subtitle: Text(message.text),
        );
      },
    );

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: fillHeight ? MainAxisSize.max : MainAxisSize.min,
          children: [
            Text(
              'Global Chat',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 16),
            if (fillHeight)
              Expanded(child: list)
            else
              SizedBox(height: 280, child: list),
          ],
        ),
      ),
    );
  }

  Future<void> _handleCreate() async {
    final room = await widget.controller.createRoom(
      widget.controller.viewModel.playerId,
      _roomController.text.trim(),
    );
    if (mounted) {
      _roomController.clear();
    }
    if (room != null) {
      await _joinRoom(room);
    }
  }

  Future<void> _joinRoom(LobbyRoom room) async {
    setState(() {
      _joining = true;
    });
    try {
      await widget.onJoinRoom(room);
    } finally {
      if (mounted) {
        setState(() {
          _joining = false;
        });
      }
    }
  }

  void _refresh() {
    if (mounted) {
      setState(() {});
    }
  }
}
