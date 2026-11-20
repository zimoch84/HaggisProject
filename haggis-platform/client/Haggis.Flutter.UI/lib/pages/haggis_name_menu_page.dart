import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../view_models/connect_view_model.dart';
import 'menu/menu_assets.dart';
import 'menu/menu_stretch_button_background.dart';

enum _MenuAction { singlePlayer, multiplayer }

class HaggisNameMenuPage extends StatefulWidget {
  const HaggisNameMenuPage({
    super.key,
    required this.viewModel,
    required this.onSinglePlayer,
    required this.onMultiplayer,
  });

  final ConnectViewModel viewModel;
  final Future<void> Function(String playerId) onSinglePlayer;
  final Future<void> Function(String playerId) onMultiplayer;

  @override
  State<HaggisNameMenuPage> createState() => _HaggisNameMenuPageState();
}

class _HaggisNameMenuPageState extends State<HaggisNameMenuPage> {
  static const Size _frontReferenceSize = Size(1154, 1363);
  static const Rect _inputRect = Rect.fromLTWH(250, 612, 654, 100);
  static const Rect _singlePlayerRect = Rect.fromLTWH(175, 784, 804, 155);
  static const Rect _multiplayerRect = Rect.fromLTWH(175, 970, 804, 155);
  static const Rect _errorRect = Rect.fromLTWH(226, 1150, 702, 90);

  late final TextEditingController _playerController;
  late final FocusNode _playerFocusNode;
  _MenuAction? _busyAction;
  String? _error;

  @override
  void initState() {
    super.initState();
    _playerController = TextEditingController(text: widget.viewModel.playerId);
    _playerFocusNode = FocusNode();
    _error = widget.viewModel.error;
  }

  @override
  void didUpdateWidget(covariant HaggisNameMenuPage oldWidget) {
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
      resizeToAvoidBottomInset: false,
      body: DecoratedBox(
        decoration: const BoxDecoration(color: Color(0xFF1B120B)),
        child: LayoutBuilder(
          builder: (BuildContext context, BoxConstraints constraints) {
            final Size frontSize = _resolveFrontSize(constraints.biggest);
            return Stack(
              fit: StackFit.expand,
              children: [
                Image.asset(MenuAssets.tableBackground, fit: BoxFit.cover),
                Center(
                  child: SizedBox.fromSize(
                    size: frontSize,
                    child: Stack(
                      fit: StackFit.expand,
                      children: [
                        Image.asset(
                          MenuAssets.nameSingleMultiBlank,
                          fit: BoxFit.fill,
                        ),
                        _PositionedFrontRect(
                          rect: _inputRect,
                          frontSize: frontSize,
                          child: _NameInput(
                            controller: _playerController,
                            focusNode: _playerFocusNode,
                            enabled: !_isBusy,
                            onChanged: _clearError,
                            onSubmitted: (_) =>
                                _start(_MenuAction.singlePlayer),
                          ),
                        ),
                        _PositionedFrontRect(
                          rect: _singlePlayerRect,
                          frontSize: frontSize,
                          child: _HotspotButton(
                            label: 'Single Player',
                            busy: _busyAction == _MenuAction.singlePlayer,
                            enabled: !_isBusy,
                            onTap: () => _start(_MenuAction.singlePlayer),
                          ),
                        ),
                        _PositionedFrontRect(
                          rect: _multiplayerRect,
                          frontSize: frontSize,
                          child: _HotspotButton(
                            label: 'Multiplayer',
                            busy: _busyAction == _MenuAction.multiplayer,
                            enabled: !_isBusy,
                            onTap: () => _start(_MenuAction.multiplayer),
                          ),
                        ),
                        if (_error != null)
                          _PositionedFrontRect(
                            rect: _errorRect,
                            frontSize: frontSize,
                            child: _ErrorPanel(message: _error!),
                          ),
                      ],
                    ),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }

  bool get _isBusy => _busyAction != null || widget.viewModel.isConnecting;

  Size _resolveFrontSize(Size available) {
    final double scale = (available.width / _frontReferenceSize.width).clamp(
      0.0,
      available.height / _frontReferenceSize.height,
    );
    return Size(
      _frontReferenceSize.width * scale,
      _frontReferenceSize.height * scale,
    );
  }

  void _clearError(String _) {
    if (_error != null) {
      setState(() {
        _error = null;
      });
    }
  }

  Future<void> _start(_MenuAction action) async {
    final String playerId = _playerController.text.trim();
    if (playerId.isEmpty) {
      const String message = 'Player name jest wymagane.';
      setState(() {
        _error = message;
      });
      await _showError(message);
      return;
    }

    setState(() {
      _busyAction = action;
      _error = null;
    });

    try {
      switch (action) {
        case _MenuAction.singlePlayer:
          await widget.onSinglePlayer(playerId);
        case _MenuAction.multiplayer:
          await widget.onMultiplayer(playerId);
      }
    } catch (error) {
      final String message = _formatError(error);
      if (mounted) {
        setState(() {
          _error = message;
        });
      }
      await _showError(message);
    } finally {
      if (mounted) {
        setState(() {
          _busyAction = null;
        });
      }
    }
  }

  Future<void> _showError(String message) async {
    if (!mounted) {
      return;
    }

    await showDialog<void>(
      context: context,
      builder: (BuildContext context) => AlertDialog(
        title: const Text('Nie można kontynuować'),
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
    final String message = error.toString();
    if (message.contains('TimeoutException')) {
      return 'Backend nie odpowiedział w wyznaczonym czasie. Sprawdź, czy Haggis Backend działa na porcie 6666.';
    }
    if (message.contains('SocketException') ||
        message.contains('WebSocketChannelException')) {
      return 'Nie można połączyć się z backendem. Sprawdź backend i adres serwera.';
    }
    return message.replaceFirst('Exception: ', '');
  }
}

class _PositionedFrontRect extends StatelessWidget {
  const _PositionedFrontRect({
    required this.rect,
    required this.frontSize,
    required this.child,
  });

  final Rect rect;
  final Size frontSize;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final Rect scaled = Rect.fromLTWH(
      rect.left *
          frontSize.width /
          _HaggisNameMenuPageState._frontReferenceSize.width,
      rect.top *
          frontSize.height /
          _HaggisNameMenuPageState._frontReferenceSize.height,
      rect.width *
          frontSize.width /
          _HaggisNameMenuPageState._frontReferenceSize.width,
      rect.height *
          frontSize.height /
          _HaggisNameMenuPageState._frontReferenceSize.height,
    );
    return Positioned.fromRect(rect: scaled, child: child);
  }
}

class _NameInput extends StatelessWidget {
  const _NameInput({
    required this.controller,
    required this.focusNode,
    required this.enabled,
    required this.onChanged,
    required this.onSubmitted,
  });

  final TextEditingController controller;
  final FocusNode focusNode;
  final bool enabled;
  final ValueChanged<String> onChanged;
  final ValueChanged<String> onSubmitted;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        return Stack(
          fit: StackFit.expand,
          children: [
            Image.asset(MenuAssets.inputTextLong, fit: BoxFit.fill),
            Padding(
              padding: EdgeInsets.symmetric(
                horizontal: constraints.maxWidth * 0.07,
              ).copyWith(top: constraints.maxHeight * 0.20, bottom: 0),
              child: TextField(
                controller: controller,
                focusNode: focusNode,
                enabled: enabled,
                textAlign: TextAlign.center,
                textAlignVertical: TextAlignVertical.center,
                textInputAction: TextInputAction.done,
                style: GoogleFonts.tangerine(
                  color: const Color(0xFF2B1708),
                  fontSize: constraints.maxHeight * 0.76,
                  fontWeight: FontWeight.w700,
                  height: 0.8,
                  letterSpacing: 0.4,
                ),
                cursorColor: const Color(0xFF5B210C),
                cursorHeight: constraints.maxHeight * 0.34,
                cursorWidth: 1.4,
                decoration: const InputDecoration(
                  border: InputBorder.none,
                  enabledBorder: InputBorder.none,
                  focusedBorder: InputBorder.none,
                  disabledBorder: InputBorder.none,
                  isCollapsed: true,
                  contentPadding: EdgeInsets.zero,
                ),
                onChanged: onChanged,
                onSubmitted: onSubmitted,
              ),
            ),
          ],
        );
      },
    );
  }
}

class _HotspotButton extends StatelessWidget {
  const _HotspotButton({
    required this.label,
    required this.busy,
    required this.enabled,
    required this.onTap,
  });

  final String label;
  final bool busy;
  final bool enabled;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: label,
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: enabled ? onTap : null,
          borderRadius: BorderRadius.circular(24),
          child: Stack(
            fit: StackFit.expand,
            alignment: Alignment.center,
            children: [
              const MenuStretchButtonBackground(),
              Center(
                child: busy
                    ? const SizedBox.square(
                        dimension: 28,
                        child: CircularProgressIndicator(strokeWidth: 3),
                      )
                    : FittedBox(
                        fit: BoxFit.scaleDown,
                        child: Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 44),
                          child: Text(
                            label,
                            textAlign: TextAlign.center,
                            style: GoogleFonts.tangerine(
                              color: const Color(0xFFFFD58A),
                              fontSize: 58,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 0.2,
                              height: 0.90,
                              shadows: const [
                                Shadow(
                                  color: Color(0xFF120700),
                                  blurRadius: 4,
                                  offset: Offset(0, 2),
                                ),
                                Shadow(
                                  color: Color(0xFF8A3B0C),
                                  blurRadius: 1,
                                  offset: Offset(0, -1),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ErrorPanel extends StatelessWidget {
  const _ErrorPanel({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: const Color(0xDD2B1007),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFE3B66A)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        child: Text(
          message,
          textAlign: TextAlign.center,
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(
            color: Color(0xFFFFE4B5),
            fontSize: 13,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
    );
  }
}
