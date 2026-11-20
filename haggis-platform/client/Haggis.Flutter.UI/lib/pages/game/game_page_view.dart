import 'package:flutter/material.dart';
import '../../controllers/game_controller.dart';
import 'game_page_layout.dart';
import 'models/card_ui_models.dart';
import 'utils/card_ui_helpers.dart';
import 'widgets/background.dart';
import 'widgets/cards.dart';
import 'widgets/game_controls.dart';
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
  final selectedMatches = controller.matchingPlayableActions;
  final readyPreviewLabel = selectedMatches.length == 1
      ? selectedMatches.first.displayAction
      : controller.selectedCards.isNotEmpty
      ? controller.selectedCards.join(' ')
      : null;
  return Scaffold(
    extendBodyBehindAppBar: true,
    body: Stack(
      children: [
        const GameBackground(),
        SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(14, 12, 14, 0),
            child: GameBoardLayout(
              handCardScale: handCardScale,
              handVerticalOffset: handVerticalOffset,
              topRibbon: TopRibbon(
                viewModel: viewModel,
                players: viewModel.players,
                onLeave: onLeave,
                hasLastRound: controller.roundOverController.hasLastRound,
                onOpenLastRound: onOpenLastRound,
                onOpenScoreHistory: onOpenScoreHistory,
                onRefresh: controller.requestSnapshot,
                onOpenHandTuning: onOpenHandTuning,
              ),
              leftControls: LayoutBuilder(
                builder: (context, constraints) => SingleChildScrollView(
                  child: ConstrainedBox(
                    constraints: BoxConstraints(
                      minHeight: constraints.maxHeight,
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        GameActionButton(
                          label: viewModel.isGameInitialized
                              ? 'Pass'
                              : 'StartGame',
                          icon: viewModel.isGameInitialized
                              ? Icons.front_hand_outlined
                              : Icons.rocket_launch_outlined,
                          onPressed: viewModel.isGameInitialized
                              ? (controller.isCurrentPlayersTurn &&
                                        controller.canPass
                                    ? controller.pass
                                    : null)
                              : (controller.room.players.length >= 2 &&
                                        controller.room.players.length <= 3
                                    ? controller.startGame
                                    : null),
                        ),
                        HandGroupControls(
                          hasSavedGroup: handSplit.groupedHand.isNotEmpty,
                          onCreateGroup:
                              viewModel.isGameInitialized &&
                                  controller.selectedCards.any(
                                    (card) =>
                                        !handSplit.groupedHand.contains(card),
                                  )
                              ? onCreateGroup
                              : null,
                          onClearGroup: onClearGroup,
                          onGroupRainbowBomb: onGroupRainbowBomb,
                          onGroupColorBomb: onGroupColorBomb,
                        ),
                      ],
                    ),
                  ),
                ),
              ),
              table: TableSection(
                viewModel: viewModel,
                readyPreviewLabel: readyPreviewLabel,
              ),
              rightControls: Align(
                alignment: Alignment.topCenter,
                child: viewModel.isGameInitialized
                    ? GameActionButton(
                        label: 'Play',
                        icon: Icons.play_arrow_rounded,
                        onPressed:
                            controller.isCurrentPlayersTurn &&
                                controller.canPlaySelectedCards
                            ? onPlaySelected
                            : null,
                      )
                    : const SizedBox.shrink(),
              ),
              hand: HandSection(
                cards: sortedHand,
                savedGroup: const [],
                playableCards: playableCards,
                selectedCards: controller.selectedCards,
                cardLabelBuilder: controller.displayCardLabel,
                onCardTap: onCardTap,
                showCardHitZones: showCardHitZones,
                cardScale: handCardScale,
                spacingScale: handSpacingScale,
                verticalOffset: handVerticalOffset,
              ),
              groupedHand: handSplit.groupedHand.isEmpty
                  ? const SizedBox.shrink()
                  : Padding(
                      padding: const EdgeInsets.fromLTRB(8, 8, 8, 4),
                      child: PlayerHandFan(
                        cards: handSplit.groupedHand,
                        playableCards: playableCards,
                        selectedCards: controller.selectedCards,
                        cardLabelBuilder: controller.displayCardLabel,
                        onCardTap: onCardTap,
                        showHitZoneOutline: showCardHitZones,
                        cardScale: handCardScale * 0.92,
                        spacingScale: handSpacingScale,
                        verticalOffset: handVerticalOffset.clamp(-12.0, 36.0),
                      ),
                    ),
              handSort: HandSortToggle(
                handSortMode: handSortMode,
                onSortChanged: onSortChanged,
              ),
            ),
          ),
        ),
      ],
    ),
  );
}
