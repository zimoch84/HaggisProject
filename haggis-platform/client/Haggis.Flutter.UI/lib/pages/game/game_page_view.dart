import 'package:flutter/material.dart';
import '../../controllers/game_controller.dart';
import 'game_page_layout.dart';
import 'models/card_ui_models.dart';
import 'utils/card_ui_helpers.dart';
import 'widgets/background.dart';
import 'widgets/hand_section.dart';
import 'widgets/table_section.dart';
import 'widgets/top_ribbon.dart';

Widget buildGamePageView({
  required BuildContext context,
  required GameController controller,
  required VoidCallback onLeave,
  required HandSortMode handSortMode,
  required ValueChanged<HandSortMode> onSortChanged,
  required List<String> savedGroup,
  required VoidCallback onCreateGroup,
  required VoidCallback onClearGroup,
  required VoidCallback? onGroupRainbowBomb,
  required VoidCallback? onGroupColorBomb,
  required bool showCardHitZones,
  required double handCardScale,
  required double handSpacingScale,
  required double handArcScale,
  required double handVerticalOffset,
  required Future<void> Function() onOpenLastRound,
  required Future<void> Function() onOpenScoreHistory,
  required Future<void> Function() onOpenHandTuning,
  required Future<void> Function() onPlaySelected,
  required Future<void> Function(String card) onCardTap,
}) {
  final viewModel = controller.viewModel;
  final handSplit = splitHandByGroupedCards(
    hand: viewModel.hand,
    groupedCards: savedGroup,
    sortMode: handSortMode,
  );
  final sortedHand = sortCardLabels(handSplit.remainingHand, handSortMode);
  final playableCards = resolvePlayableCards(controller, viewModel);
  return Scaffold(
    extendBodyBehindAppBar: true,
    body: Stack(
      children: [
        const GameBackground(),
        SafeArea(
          bottom: false,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(14, 12, 14, 0),
            child: LayoutBuilder(
              builder: (BuildContext context, BoxConstraints constraints) {
                final isLandscape = isLandscapeLayout(constraints);
                final reservedTopHeight = isLandscape ? 112.0 : 124.0;
                final reservedTableGap = isLandscape ? 22.0 : 26.0;
                final handSectionHeight = computeHandSectionHeight(
                  availableHeight:
                      constraints.maxHeight -
                      reservedTopHeight -
                      reservedTableGap,
                  isLandscape: isLandscape,
                  handCardScale: handCardScale,
                  handArcScale: handArcScale,
                  handVerticalOffset: handVerticalOffset,
                );
                if (isLandscape) {
                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      TopRibbon(
                        viewModel: viewModel,
                        players: viewModel.players,
                        onLeave: onLeave,
                        hasLastRound:
                            controller.roundOverController.hasLastRound,
                        onOpenLastRound: onOpenLastRound,
                        onOpenScoreHistory: onOpenScoreHistory,
                        onRefresh: controller.requestSnapshot,
                        onOpenHandTuning: onOpenHandTuning,
                      ),
                      const SizedBox(height: 4),
                      Expanded(
                        child: SurfaceCard(
                          padding: const EdgeInsets.fromLTRB(18, 4, 18, 14),
                          backgroundColor: Colors.transparent,
                          borderColor: Colors.transparent,
                          boxShadow: const [],
                          child: TableSection(
                            viewModel: viewModel,
                            controller: controller,
                            onPlayPressed: onPlaySelected,
                          ),
                        ),
                      ),
                      const SizedBox(height: 14),
                      SizedBox(
                        height: handSectionHeight,
                        child: HandSection(
                          cards: sortedHand,
                          savedGroup: handSplit.groupedHand,
                          playableCards: playableCards,
                          selectedCards: controller.selectedCards,
                          cardLabelBuilder: controller.displayCardLabel,
                          onCardTap: onCardTap,
                          handSortMode: handSortMode,
                          onSortChanged: onSortChanged,
                          onCreateGroup: onCreateGroup,
                          onClearGroup: onClearGroup,
                          onGroupRainbowBomb: onGroupRainbowBomb,
                          onGroupColorBomb: onGroupColorBomb,
                          showCardHitZones: showCardHitZones,
                          cardScale: handCardScale,
                          spacingScale: handSpacingScale,
                          arcScale: handArcScale,
                          verticalOffset: handVerticalOffset,
                        ),
                      ),
                    ],
                  );
                }
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    TopRibbon(
                      viewModel: viewModel,
                      players: viewModel.players,
                      onLeave: onLeave,
                      hasLastRound: controller.roundOverController.hasLastRound,
                      onOpenLastRound: onOpenLastRound,
                      onOpenScoreHistory: onOpenScoreHistory,
                      onRefresh: controller.requestSnapshot,
                      onOpenHandTuning: onOpenHandTuning,
                    ),
                    const SizedBox(height: 4),
                    Expanded(
                      child: SurfaceCard(
                        padding: const EdgeInsets.fromLTRB(18, 4, 18, 14),
                        backgroundColor: Colors.transparent,
                        borderColor: Colors.transparent,
                        boxShadow: const [],
                        child: TableSection(
                          viewModel: viewModel,
                          controller: controller,
                          onPlayPressed: onPlaySelected,
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),
                    SizedBox(
                      height: handSectionHeight,
                      child: HandSection(
                        cards: sortedHand,
                        savedGroup: handSplit.groupedHand,
                        playableCards: playableCards,
                        selectedCards: controller.selectedCards,
                        cardLabelBuilder: controller.displayCardLabel,
                        onCardTap: onCardTap,
                        handSortMode: handSortMode,
                        onSortChanged: onSortChanged,
                        onCreateGroup: onCreateGroup,
                        onClearGroup: onClearGroup,
                        onGroupRainbowBomb: onGroupRainbowBomb,
                        onGroupColorBomb: onGroupColorBomb,
                        showCardHitZones: showCardHitZones,
                        cardScale: handCardScale,
                        spacingScale: handSpacingScale,
                        arcScale: handArcScale,
                        verticalOffset: handVerticalOffset,
                      ),
                    ),
                  ],
                );
              },
            ),
          ),
        ),
      ],
    ),
  );
}
