import 'package:flutter/material.dart';

class GameActionButton extends StatelessWidget {
  const GameActionButton({
    super.key,
    required this.label,
    required this.icon,
    required this.onPressed,
  });

  final String label;
  final IconData icon;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return FittedBox(
      fit: BoxFit.scaleDown,
      child: FilledButton.icon(
        onPressed: onPressed,
        style: FilledButton.styleFrom(
          backgroundColor: const Color(0xFFB15D45),
          disabledBackgroundColor: const Color(0xFF5C4944),
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
        icon: Icon(icon),
        label: Text(label, style: const TextStyle(fontWeight: FontWeight.w800)),
      ),
    );
  }
}

class HandGroupControls extends StatelessWidget {
  const HandGroupControls({
    super.key,
    required this.hasSavedGroup,
    required this.onCreateGroup,
    required this.onClearGroup,
    required this.onGroupRainbowBomb,
    required this.onGroupColorBomb,
  });

  final bool hasSavedGroup;
  final VoidCallback? onCreateGroup;
  final VoidCallback onClearGroup;
  final VoidCallback? onGroupRainbowBomb;
  final VoidCallback? onGroupColorBomb;

  @override
  Widget build(BuildContext context) {
    final style = FilledButton.styleFrom(
      backgroundColor: const Color(0x66163034),
      foregroundColor: Colors.white,
      side: const BorderSide(color: Color(0x6656B891)),
      shape: const StadiumBorder(),
    );
    Widget button(String label, VoidCallback callback) => Padding(
      padding: const EdgeInsets.only(top: 4),
      child: FittedBox(
        fit: BoxFit.scaleDown,
        child: FilledButton.tonal(
          style: style,
          onPressed: callback,
          child: Text(label),
        ),
      ),
    );
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (onCreateGroup != null) button('Group', onCreateGroup!),
        if (hasSavedGroup)
          button('Reset group', onClearGroup)
        else ...[
          if (onGroupRainbowBomb != null)
            button('Rainbow', onGroupRainbowBomb!),
          if (onGroupColorBomb != null)
            button('Color (bomb)', onGroupColorBomb!),
        ],
      ],
    );
  }
}
