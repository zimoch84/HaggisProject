import 'package:flutter/widgets.dart';

import 'menu_assets.dart';

class MenuStretchButtonBackground extends StatelessWidget {
  const MenuStretchButtonBackground({
    super.key,
    this.leftAsset = MenuAssets.buttonRedLeft,
    this.centerAsset = MenuAssets.buttonRedCenter,
    this.rightAsset = MenuAssets.buttonRedRight,
    this.capWidthRatio = 190 / 887,
  });

  final String leftAsset;
  final String centerAsset;
  final String rightAsset;
  final double capWidthRatio;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        final double resolvedCapWidth = (constraints.maxWidth * capWidthRatio)
            .clamp(0, constraints.maxWidth / 2);

        return Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SizedBox(
              width: resolvedCapWidth,
              child: Image.asset(leftAsset, fit: BoxFit.fill),
            ),
            Expanded(child: Image.asset(centerAsset, fit: BoxFit.fill)),
            SizedBox(
              width: resolvedCapWidth,
              child: Image.asset(rightAsset, fit: BoxFit.fill),
            ),
          ],
        );
      },
    );
  }
}
