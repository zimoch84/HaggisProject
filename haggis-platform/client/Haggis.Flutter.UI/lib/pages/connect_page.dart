import 'package:flutter/material.dart';

import '../view_models/connect_view_model.dart';

class ConnectPage extends StatefulWidget {
  const ConnectPage({
    super.key,
    required this.viewModel,
    required this.onPlayerIdChanged,
    required this.onConnect,
  });

  final ConnectViewModel viewModel;
  final ValueChanged<String> onPlayerIdChanged;
  final Future<void> Function() onConnect;

  @override
  State<ConnectPage> createState() => _ConnectPageState();
}

class _ConnectPageState extends State<ConnectPage> {
  late final TextEditingController _playerController;
  bool _connecting = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _playerController = TextEditingController(text: widget.viewModel.playerId);
    _error = widget.viewModel.error;
  }

  @override
  void didUpdateWidget(covariant ConnectPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel.playerId != widget.viewModel.playerId &&
        _playerController.text != widget.viewModel.playerId) {
      _playerController.text = widget.viewModel.playerId;
    }
    _error = widget.viewModel.error;
  }

  @override
  void dispose() {
    _playerController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 520),
          child: Card(
            margin: const EdgeInsets.all(24),
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Haggis',
                    style: Theme.of(context).textTheme.headlineMedium,
                  ),
                  const SizedBox(height: 8),
                  const SizedBox(height: 24),
                  TextField(
                    controller: _playerController,
                    decoration: const InputDecoration(labelText: 'Player ID'),
                  ),
                  const SizedBox(height: 12),
                  if (_error != null) ...[
                    const SizedBox(height: 16),
                    Text(
                      _error!,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                  ],
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: (_connecting || widget.viewModel.isConnecting)
                        ? null
                        : _handleConnect,
                    child: Text(
                      (_connecting || widget.viewModel.isConnecting)
                          ? 'Connecting...'
                          : 'Open Lobby',
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _handleConnect() async {
    final playerId = _playerController.text.trim();
    if (playerId.isEmpty) {
      setState(() {
        _error = 'Player ID jest wymagane.';
      });
      return;
    }

    widget.onPlayerIdChanged(playerId);
    setState(() {
      _connecting = true;
      _error = null;
    });

    try {
      await widget.onConnect();
    } catch (error) {
      setState(() {
        _error = error.toString();
      });
    } finally {
      if (mounted) {
        setState(() {
          _connecting = false;
        });
      }
    }
  }
}
