import 'package:flutter/material.dart';
import '../../view_models/game_view_model.dart';

bool isLandscapeLayout(BoxConstraints constraints) =>
    constraints.maxWidth > constraints.maxHeight;

double computeHandSectionHeight({
  required double availableHeight,
  required bool isLandscape,
  required double handCardScale,
  required double handVerticalOffset,
}) {
  final cardHeight = 98.0 * handCardScale;
  final fanExtraHeight = 48.0 * handCardScale;
  final verticalTravel = handVerticalOffset.abs();
  const bottomPadding = 0.0;
  const safetyMargin = 0.0;
  const topPadding = 48.0;
  final computed =
      topPadding +
      cardHeight +
      fanExtraHeight +
      verticalTravel +
      bottomPadding +
      safetyMargin;
  final safeAvailableHeight = availableHeight.clamp(240.0, double.infinity);
  final viewportCap = isLandscape
      ? (safeAvailableHeight * 0.44) - 12.0
      : (safeAvailableHeight * 0.38) - 8.0;
  final hardCap = isLandscape ? 240.0 : 260.0;
  final maxAllowed = viewportCap.clamp(148.0, hardCap);
  return computed.clamp(148.0, maxAllowed);
}

GamePlayerViewModel? resolveOwnPlayer(GameViewModel viewModel) {
  final players = viewModel.players.where(
    (GamePlayerViewModel player) => player.id == viewModel.playerId,
  );
  return players.isEmpty ? null : players.first;
}

List<GamePlayerViewModel> resolveOpponents(GameViewModel viewModel) {
  return viewModel.players
      .where((GamePlayerViewModel player) => player.id != viewModel.playerId)
      .toList(growable: false);
}

/// The ribbon stays above the board; the hand has its own bottom area.
class GameBoardLayout extends StatelessWidget {
  const GameBoardLayout({
    super.key,
    required this.topRibbon,
    required this.leftControls,
    required this.table,
    required this.rightControls,
    required this.hand,
    required this.handSort,
    this.groupedHand = const SizedBox.shrink(),
    required this.handCardScale,
    required this.handVerticalOffset,
  });

  final Widget topRibbon;
  final Widget leftControls;
  final Widget table;
  final Widget rightControls;
  final Widget hand;
  final Widget handSort;
  final Widget groupedHand;
  final double handCardScale;
  final double handVerticalOffset;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final landscape = isLandscapeLayout(constraints);
        final sideWidth = (constraints.maxWidth * 0.15).clamp(64.0, 160.0);
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            topRibbon,
            const SizedBox(height: 12),
            Expanded(
              child: LayoutBuilder(
                builder: (context, boardConstraints) {
                  final handHeight = computeHandSectionHeight(
                    availableHeight: boardConstraints.maxHeight,
                    isLandscape: landscape,
                    handCardScale: handCardScale,
                    handVerticalOffset: handVerticalOffset,
                  ).clamp(0.0, boardConstraints.maxHeight * 0.5);
                  return Column(
                    children: [
                      Expanded(
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            SizedBox(width: sideWidth, child: leftControls),
                            const SizedBox(width: 8),
                            Expanded(child: table),
                            const SizedBox(width: 8),
                            SizedBox(width: sideWidth, child: rightControls),
                          ],
                        ),
                      ),
                      const SizedBox(height: 8),
                      SizedBox(
                        height: handHeight,
                        child: landscape
                            ? Row(
                                children: [
                                  SizedBox(
                                    width: sideWidth,
                                    child: groupedHand,
                                  ),
                                  const SizedBox(width: 8),
                                  Expanded(child: hand),
                                  const SizedBox(width: 8),
                                  SizedBox(
                                    width: sideWidth,
                                    child: FittedBox(
                                      fit: BoxFit.scaleDown,
                                      child: handSort,
                                    ),
                                  ),
                                ],
                              )
                            : Column(
                                children: [
                                  Expanded(
                                    child: Row(
                                      children: [
                                        SizedBox(
                                          width: sideWidth,
                                          child: groupedHand,
                                        ),
                                        const SizedBox(width: 8),
                                        Expanded(child: hand),
                                        SizedBox(width: sideWidth + 8),
                                      ],
                                    ),
                                  ),
                                  Align(
                                    alignment: Alignment.centerRight,
                                    child: handSort,
                                  ),
                                ],
                              ),
                      ),
                    ],
                  );
                },
              ),
            ),
          ],
        );
      },
    );
  }
}
