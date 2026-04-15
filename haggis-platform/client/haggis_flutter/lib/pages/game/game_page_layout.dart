import 'package:flutter/material.dart';
import '../../view_models/game_view_model.dart';

bool isLandscapeLayout(BoxConstraints constraints) =>
    constraints.maxWidth > constraints.maxHeight;

double computeHandSectionHeight({
  required double availableHeight,
  required bool isLandscape,
  required double handCardScale,
  required double handArcScale,
  required double handVerticalOffset,
}) {
  final cardHeight = 98.0 * handCardScale;
  final fanExtraHeight = 36.0 + (18.0 * handArcScale);
  final verticalTravel = handVerticalOffset > 0 ? handVerticalOffset : 0.0;
  const bottomPadding = 18.0;
  const safetyMargin = 12.0;
  const topPadding = 48.0;
  final computed =
      topPadding +
      cardHeight +
      fanExtraHeight +
      verticalTravel +
      bottomPadding +
      safetyMargin;
  final viewportCap = isLandscape
      ? (availableHeight * 0.48) - 12.0
      : (availableHeight * 0.38) - 8.0;
  final hardCap = isLandscape ? 300.0 : 260.0;
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
