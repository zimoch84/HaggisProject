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
