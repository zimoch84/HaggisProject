import 'package:flutter/material.dart';

import '../view_models/connect_view_model.dart';

class ConnectPage extends StatefulWidget {
  const ConnectPage({
    super.key,
    required this.viewModel,
    required this.onConnect,
  });

  final ConnectViewModel viewModel;
  final Future<void> Function(String playerId) onConnect;

  @override
  State<ConnectPage> createState() => _ConnectPageState();
}

class _ConnectPageState extends State<ConnectPage> {
  late final TextEditingController _playerController;
  late final FocusNode _playerFocusNode;
  bool _connecting = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _playerController = TextEditingController(text: widget.viewModel.playerId);
    _playerFocusNode = FocusNode();
    _error = widget.viewModel.error;

    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        _playerFocusNode.requestFocus();
      }
    });
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
    _playerFocusNode.dispose();
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
                    focusNode: _playerFocusNode,
                    autofocus: true,
                    decoration: const InputDecoration(labelText: 'Player ID'),
                    textInputAction: TextInputAction.done,
                    onChanged: (_) {
                      if (_error != null) {
                        setState(() {
                          _error = null;
                        });
                      }
                    },
                    onSubmitted: (_) => _handleConnect(),
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
      await _showConnectError(_error!);
      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Otwieram lobby jako "$playerId"...')),
    );

    setState(() {
      _connecting = true;
      _error = null;
    });

    try {
      await widget.onConnect(playerId);
    } catch (error) {
      final message = _formatError(error);
      setState(() {
        _error = message;
      });
      await _showConnectError(message);
    } finally {
      if (mounted) {
        setState(() {
          _connecting = false;
        });
      }
    }
  }

  Future<void> _showConnectError(String message) async {
    if (!mounted) {
      return;
    }

    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Nie można otworzyć lobby'),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('OK'),
          ),
        ],
      ),
    );
  }

  String _formatError(Object error) {
    final message = error.toString();
    if (message.contains('TimeoutException')) {
      return 'Backend nie odpowiedział w ciągu 5 sekund. Sprawdź, czy Haggis Backend działa na porcie 6666.';
    }
    if (message.contains('SocketException') ||
        message.contains('WebSocketChannelException')) {
      return 'Nie można połączyć się z backendem lobby. Sprawdź backend i adres serwera.';
    }
    return message.replaceFirst('Exception: ', '');
  }
}
