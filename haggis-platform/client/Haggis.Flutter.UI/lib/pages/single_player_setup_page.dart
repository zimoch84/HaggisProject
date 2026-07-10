import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../models/single_player_models.dart';
import 'menu/menu_assets.dart';
import 'menu/menu_reference_layout.dart';
import 'menu/menu_stretch_button_background.dart';

class SinglePlayerSetupPage extends StatefulWidget {
  const SinglePlayerSetupPage({
    super.key,
    required this.playerId,
    required this.onStart,
    required this.onBack,
  });

  final String playerId;
  final Future<void> Function(List<SinglePlayerAiConfig> aiPlayers) onStart;
  final VoidCallback onBack;

  @override
  State<SinglePlayerSetupPage> createState() => _SinglePlayerSetupPageState();
}

class _SinglePlayerSetupPageState extends State<SinglePlayerSetupPage> {
  static const double _referenceCenterX = 836;
  static const double _optionWidth = 210;
  static const double _optionHeight = 76;
  static const double _optionColumnGap = 25;
  static const double _optionColumnOffset = _optionWidth + _optionColumnGap;
  static const double _optionGroupWidth = _optionWidth * 2 + _optionColumnGap;
  static const double _optionGroupGap = 82;
  static const double _ai1Left =
      _referenceCenterX - _optionGroupGap / 2 - _optionGroupWidth;
  static const double _ai2Left = _referenceCenterX + _optionGroupGap / 2;
  static const double _row1Y = 420;
  static const double _row2Y = 520;
  static const double _row3Y = 620;

  static const Rect _separatorRect = Rect.fromLTWH(834, 342, 4, 318);
  static const Rect _ai1TitleRect = Rect.fromLTWH(
    _ai1Left,
    382,
    _optionGroupWidth,
    42,
  );
  static const Rect _ai2TitleRect = Rect.fromLTWH(
    _ai2Left,
    382,
    _optionGroupWidth,
    42,
  );
  static const Rect _startRect = Rect.fromLTWH(640, 730, 390, 84);
  static const Rect _backRect = Rect.fromLTWH(705, 835, 260, 62);

  static const List<_DifficultyOption> _ai1Options = <_DifficultyOption>[
    _DifficultyOption(
      label: 'Easy',
      difficulty: AiDifficulty.easy,
      rect: Rect.fromLTWH(_ai1Left, _row1Y, _optionWidth, _optionHeight),
    ),
    _DifficultyOption(
      label: 'Normal',
      difficulty: AiDifficulty.normal,
      rect: Rect.fromLTWH(
        _ai1Left + _optionColumnOffset,
        _row1Y,
        _optionWidth,
        _optionHeight,
      ),
    ),
    _DifficultyOption(
      label: 'Medium',
      difficulty: AiDifficulty.medium,
      rect: Rect.fromLTWH(_ai1Left, _row2Y, _optionWidth, _optionHeight),
    ),
    _DifficultyOption(
      label: 'Hard',
      difficulty: AiDifficulty.hard,
      rect: Rect.fromLTWH(
        _ai1Left + _optionColumnOffset,
        _row2Y,
        _optionWidth,
        _optionHeight,
      ),
    ),
    _DifficultyOption(
      label: 'Expert',
      difficulty: AiDifficulty.expert,
      rect: Rect.fromLTWH(_ai1Left, _row3Y, _optionWidth, _optionHeight),
    ),
  ];

  static const List<_DifficultyOption> _ai2Options = <_DifficultyOption>[
    _DifficultyOption(
      label: 'Easy',
      difficulty: AiDifficulty.easy,
      rect: Rect.fromLTWH(_ai2Left, _row1Y, _optionWidth, _optionHeight),
    ),
    _DifficultyOption(
      label: 'Normal',
      difficulty: AiDifficulty.normal,
      rect: Rect.fromLTWH(
        _ai2Left + _optionColumnOffset,
        _row1Y,
        _optionWidth,
        _optionHeight,
      ),
    ),
    _DifficultyOption(
      label: 'Medium',
      difficulty: AiDifficulty.medium,
      rect: Rect.fromLTWH(_ai2Left, _row2Y, _optionWidth, _optionHeight),
    ),
    _DifficultyOption(
      label: 'Hard',
      difficulty: AiDifficulty.hard,
      rect: Rect.fromLTWH(
        _ai2Left + _optionColumnOffset,
        _row2Y,
        _optionWidth,
        _optionHeight,
      ),
    ),
    _DifficultyOption(
      label: 'Expert',
      difficulty: AiDifficulty.expert,
      rect: Rect.fromLTWH(_ai2Left, _row3Y, _optionWidth, _optionHeight),
    ),
    _DifficultyOption(
      label: 'None',
      rect: Rect.fromLTWH(
        _ai2Left + _optionColumnOffset,
        _row3Y,
        _optionWidth,
        _optionHeight,
      ),
    ),
  ];

  AiDifficulty _ai1Difficulty = AiDifficulty.normal;
  AiDifficulty? _ai2Difficulty = AiDifficulty.normal;
  bool _starting = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      resizeToAvoidBottomInset: false,
      backgroundColor: const Color(0xFF100905),
      body: LayoutBuilder(
        builder: (BuildContext context, BoxConstraints constraints) {
          final Size canvasSize = _resolveCanvasSize(constraints.biggest);

          return Center(
            child: SizedBox.fromSize(
              size: canvasSize,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  Image.asset(MenuAssets.tableBackground, fit: BoxFit.fill),
                  Image.asset(MenuAssets.haggisBoard16x9, fit: BoxFit.fill),
                  _ReferencePositioned(
                    rect: _separatorRect,
                    canvasSize: canvasSize,
                    child: const _AiSeparator(),
                  ),
                  _ReferencePositioned(
                    rect: _ai1TitleRect,
                    canvasSize: canvasSize,
                    child: const _SectionTitle('AI-1'),
                  ),
                  _ReferencePositioned(
                    rect: _ai2TitleRect,
                    canvasSize: canvasSize,
                    child: const _SectionTitle('AI-2'),
                  ),
                  for (final _DifficultyOption option in _ai1Options)
                    _ReferencePositioned(
                      rect: option.rect,
                      canvasSize: canvasSize,
                      child: _MenuAssetButton(
                        label: option.label,
                        selected: _ai1Difficulty == option.difficulty,
                        enabled: !_starting,
                        onTap: () => _selectAi1(option.difficulty!),
                      ),
                    ),
                  for (final _DifficultyOption option in _ai2Options)
                    _ReferencePositioned(
                      rect: option.rect,
                      canvasSize: canvasSize,
                      child: _MenuAssetButton(
                        label: option.label,
                        selected: _ai2Difficulty == option.difficulty,
                        enabled: !_starting,
                        onTap: () => _selectAi2(option.difficulty),
                      ),
                    ),
                  _ReferencePositioned(
                    rect: _startRect,
                    canvasSize: canvasSize,
                    child: _MenuAssetButton(
                      label: _starting ? 'Starting...' : 'Start',
                      selected: false,
                      enabled: !_starting,
                      onTap: _start,
                      fontSize: 86,
                      horizontalPadding: 42,
                    ),
                  ),
                  _ReferencePositioned(
                    rect: _backRect,
                    canvasSize: canvasSize,
                    child: _MenuAssetButton(
                      label: 'Back',
                      selected: false,
                      enabled: !_starting,
                      onTap: widget.onBack,
                      fontSize: 58,
                      horizontalPadding: 28,
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Size _resolveCanvasSize(Size available) {
    final Size referenceSize = MenuReferenceLayout.referenceSize;
    final double scale = (available.width / referenceSize.width).clamp(
      0.0,
      available.height / referenceSize.height,
    );
    return Size(referenceSize.width * scale, referenceSize.height * scale);
  }

  void _selectAi1(AiDifficulty difficulty) {
    setState(() {
      _ai1Difficulty = difficulty;
    });
  }

  void _selectAi2(AiDifficulty? difficulty) {
    setState(() {
      _ai2Difficulty = difficulty;
    });
  }

  Future<void> _start() async {
    final List<SinglePlayerAiConfig> aiPlayers = <SinglePlayerAiConfig>[
      SinglePlayerAiConfig(name: 'AI-1', difficulty: _ai1Difficulty),
      if (_ai2Difficulty != null)
        SinglePlayerAiConfig(name: 'AI-2', difficulty: _ai2Difficulty!),
    ];

    setState(() {
      _starting = true;
    });
    try {
      await widget.onStart(List<SinglePlayerAiConfig>.unmodifiable(aiPlayers));
    } finally {
      if (mounted) {
        setState(() {
          _starting = false;
        });
      }
    }
  }
}

class _DifficultyOption {
  const _DifficultyOption({
    required this.label,
    required this.rect,
    this.difficulty,
  });

  final String label;
  final Rect rect;
  final AiDifficulty? difficulty;
}

class _ReferencePositioned extends StatelessWidget {
  const _ReferencePositioned({
    required this.rect,
    required this.canvasSize,
    required this.child,
  });

  final Rect rect;
  final Size canvasSize;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Positioned.fromRect(
      rect: MenuReferenceLayout.scaleRect(rect, canvasSize),
      child: child,
    );
  }
}

class _AiSeparator extends StatelessWidget {
  const _AiSeparator();

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: const Color(0x66D6A44A),
          borderRadius: BorderRadius.circular(4),
          boxShadow: const [
            BoxShadow(
              color: Color(0xAA120700),
              blurRadius: 8,
              offset: Offset(0, 0),
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.center,
      child: FittedBox(
        fit: BoxFit.scaleDown,
        child: Text(
          text,
          style: GoogleFonts.tangerine(
            color: const Color(0xFFE33624),
            fontSize: 48,
            fontWeight: FontWeight.w700,
            shadows: const [
              Shadow(
                color: Color(0xFF120700),
                blurRadius: 4,
                offset: Offset(0, 2),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _MenuAssetButton extends StatelessWidget {
  const _MenuAssetButton({
    required this.label,
    required this.selected,
    required this.enabled,
    required this.onTap,
    this.fontSize = 56,
    this.horizontalPadding = 20,
  });

  final String label;
  final bool selected;
  final bool enabled;
  final VoidCallback? onTap;
  final double fontSize;
  final double horizontalPadding;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: selected,
      enabled: enabled,
      label: label,
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: enabled ? onTap : null,
          borderRadius: BorderRadius.circular(18),
          child: Opacity(
            opacity: enabled ? 1 : 0.55,
            child: Stack(
              fit: StackFit.expand,
              alignment: Alignment.center,
              children: [
                const MenuStretchButtonBackground(),
                if (selected) const _SelectedButtonGlow(),
                Center(
                  child: FittedBox(
                    fit: BoxFit.scaleDown,
                    child: Padding(
                      padding: EdgeInsets.symmetric(
                        horizontal: horizontalPadding,
                      ),
                      child: Text(
                        label,
                        textAlign: TextAlign.center,
                        style: GoogleFonts.tangerine(
                          color: selected
                              ? const Color(0xFFFFFFFF)
                              : const Color(0xFFFFD58A),
                          fontSize: fontSize,
                          fontWeight: FontWeight.w700,
                          height: 0.9,
                          shadows: const [
                            Shadow(
                              color: Color(0xFF120700),
                              blurRadius: 4,
                              offset: Offset(0, 2),
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
      ),
    );
  }
}

class _SelectedButtonGlow extends StatelessWidget {
  const _SelectedButtonGlow();

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: DecoratedBox(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: const Color(0xFFFFE7A5), width: 3),
          boxShadow: const [
            BoxShadow(color: Color(0xAAFFD36A), blurRadius: 14),
            BoxShadow(color: Color(0x66FFFFFF), blurRadius: 5),
          ],
        ),
      ),
    );
  }
}
