import 'package:flutter/material.dart';

import '../../menu/menu_assets.dart';

class GameBackground extends StatelessWidget {
  const GameBackground({super.key});

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Positioned.fill(
          child: Image.asset(
            MenuAssets.tableBackground,
            fit: BoxFit.cover,
            alignment: Alignment.center,
          ),
        ),
        Positioned.fill(
          child: DecoratedBox(
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [
                  const Color(0x55102124),
                  const Color(0x77102124),
                  const Color(0x99102124),
                ],
              ),
            ),
          ),
        ),
        Positioned.fill(child: CustomPaint(painter: NoisePainter())),
      ],
    );
  }
}

class NoisePainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()..color = const Color(0x05000000);
    const double spacing = 22;

    for (double x = 0; x < size.width; x += spacing) {
      canvas.drawRect(Rect.fromLTWH(x, 0, 1, size.height), paint);
    }

    for (double y = 0; y < size.height; y += spacing) {
      canvas.drawRect(Rect.fromLTWH(0, y, size.width, 1), paint);
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
