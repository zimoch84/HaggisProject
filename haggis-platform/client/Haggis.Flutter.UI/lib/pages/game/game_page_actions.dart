import 'package:flutter/material.dart';

import '../../controllers/game_controller.dart';
import '../round_over_page.dart';
import '../score_history_page.dart';
import 'widgets/hand_section.dart';

Future<void> openScoreHistory({
  required BuildContext context,
  required GameController controller,
}) async {
  final viewModel = controller.scoreHistoryController.viewModel;
  if (viewModel == null) {
    return;
  }
  await Navigator.of(context).push(
    MaterialPageRoute<void>(
      builder: (BuildContext context) => ScoreHistoryPage(viewModel: viewModel),
    ),
  );
}

Future<void> openLastRound({
  required BuildContext context,
  required GameController controller,
}) async {
  final viewModel = controller.roundOverController.lastRound;
  if (viewModel == null) {
    return;
  }
  await Navigator.of(context).push(
    MaterialPageRoute<void>(
      builder: (BuildContext context) => RoundOverPage(viewModel: viewModel),
    ),
  );
}

Future<void> handlePlaySelected({
  required BuildContext context,
  required GameController controller,
}) async {
  var matches = controller.matchingPlayableActions;
  if (matches.isEmpty) {
    return;
  }
  if (matches.length == 1) {
    controller.playSelectedCards(matches.first);
    return;
  }
  for (final String card in controller.selectedCards) {
    if (!controller.isWildCard(card)) {
      continue;
    }
    final options = controller.getWildReplacementOptions(card);
    if (options.length > 1 && !controller.wildAssignments.containsKey(card)) {
      await showWildAssignmentPopup(
        context: context,
        controller: controller,
        wildCard: card,
      );
      return;
    }
  }
  matches = controller.matchingPlayableActions;
  if (matches.length == 1) {
    controller.playSelectedCards(matches.first);
  }
}

Future<void> handleCardTap({
  required BuildContext context,
  required GameController controller,
  required String card,
}) async {
  final wasSelected = controller.isCardSelected(card);
  if (wasSelected) {
    if (controller.isWildCard(card)) {
      final options = controller.getWildReplacementOptions(card);
      if (options.length > 1) {
        await showWildAssignmentPopup(
          context: context,
          controller: controller,
          wildCard: card,
        );
        return;
      }
    }
    controller.toggleSelectedCard(card);
    return;
  }
  controller.toggleSelectedCard(card);
  if (controller.isWildCard(card)) {
    final options = controller.getWildReplacementOptions(card);
    if (options.length > 1) {
      await showWildAssignmentPopup(
        context: context,
        controller: controller,
        wildCard: card,
      );
    }
  }
}

Future<void> showWildAssignmentPopup({
  required BuildContext context,
  required GameController controller,
  required String wildCard,
  List<String>? cards,
  Map<String, String>? wildAssignments,
}) async {
  final options = cards == null
      ? controller.getWildReplacementOptions(wildCard)
      : controller.getWildReplacementOptionsForCards(
          wildCard,
          cards,
          wildAssignments: wildAssignments,
        );
  if (options.length <= 1) {
    return;
  }
  final chosen = await showDialog<String>(
    context: context,
    builder: (BuildContext context) {
      return AlertDialog(
        backgroundColor: const Color(0xFF173035),
        title: Text(
          'Wybierz dla $wildCard',
          style: const TextStyle(color: Colors.white),
        ),
        content: SizedBox(
          width: double.maxFinite,
          child: Wrap(
            spacing: 10,
            runSpacing: 10,
            children: options
                .map(
                  (String option) => FilledButton.tonal(
                    onPressed: () => Navigator.of(context).pop(option),
                    child: Text('$wildCard[$option]'),
                  ),
                )
                .toList(growable: false),
          ),
        ),
        actions: [
          if (controller.isCardSelected(wildCard))
            TextButton(
              onPressed: () => Navigator.of(context).pop('__remove__'),
              child: const Text('Odznacz'),
            ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Anuluj'),
          ),
        ],
      );
    },
  );
  if (chosen == '__remove__') {
    controller.toggleSelectedCard(wildCard);
    return;
  }
  if (chosen != null && chosen.isNotEmpty) {
    controller.setWildReplacement(wildCard, chosen);
  }
}

void handleRefresh({
  required bool mounted,
  required BuildContext context,
  required GameController controller,
  required VoidCallback onRebuild,
}) {
  if (!mounted) {
    return;
  }
  final pendingRound = controller.roundOverController.consumePendingRound();
  onRebuild();
  if (pendingRound != null) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        Navigator.of(context).push(
          MaterialPageRoute<void>(
            builder: (BuildContext context) =>
                RoundOverPage(viewModel: pendingRound),
          ),
        );
      }
    });
  }
}

Future<void> openHandTuning({
  required BuildContext context,
  required double handCardScale,
  required double handSpacingScale,
  required double handArcScale,
  required double handVerticalOffset,
  required bool showCardHitZones,
  required ValueChanged<double> onCardScaleChanged,
  required ValueChanged<double> onSpacingScaleChanged,
  required ValueChanged<double> onArcScaleChanged,
  required ValueChanged<double> onVerticalOffsetChanged,
  required ValueChanged<bool> onShowCardHitZonesChanged,
}) async {
  await showDialog<void>(
    context: context,
    builder: (BuildContext context) {
      final screenSize = MediaQuery.of(context).size;
      double cardScale = handCardScale;
      double spacingScale = handSpacingScale;
      double arcScale = handArcScale;
      double verticalOffset = handVerticalOffset;
      bool cardHitZones = showCardHitZones;

      return AlertDialog(
        backgroundColor: const Color(0xEE162A2E),
        insetPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
        contentPadding: const EdgeInsets.fromLTRB(18, 10, 18, 8),
        actionsPadding: const EdgeInsets.fromLTRB(12, 0, 12, 10),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(20),
          side: const BorderSide(color: Color(0x6656B891)),
        ),
        title: const Text(
          'Ustawienia kart',
          style: TextStyle(color: Colors.white),
        ),
        content: StatefulBuilder(
          builder: (BuildContext context, StateSetter setModalState) {
            void updateValues(VoidCallback update) {
              update();
              setModalState(() {});
            }

            return ConstrainedBox(
              constraints: BoxConstraints(
                maxWidth: screenSize.width * 0.78,
                maxHeight: screenSize.height * 0.58,
              ),
              child: SizedBox(
                width: 560,
                child: SingleChildScrollView(
                  child: HandTuningPanel(
                    cardScale: cardScale,
                    spacingScale: spacingScale,
                    arcScale: arcScale,
                    verticalOffset: verticalOffset,
                    showCardHitZones: cardHitZones,
                    onCardScaleChanged: (double value) {
                      updateValues(() {
                        cardScale = value;
                      });
                      onCardScaleChanged(value);
                    },
                    onSpacingScaleChanged: (double value) {
                      updateValues(() {
                        spacingScale = value;
                      });
                      onSpacingScaleChanged(value);
                    },
                    onArcScaleChanged: (double value) {
                      updateValues(() {
                        arcScale = value;
                      });
                      onArcScaleChanged(value);
                    },
                    onVerticalOffsetChanged: (double value) {
                      updateValues(() {
                        verticalOffset = value;
                      });
                      onVerticalOffsetChanged(value);
                    },
                    onShowCardHitZonesChanged: (bool value) {
                      updateValues(() {
                        cardHitZones = value;
                      });
                      onShowCardHitZonesChanged(value);
                    },
                  ),
                ),
              ),
            );
          },
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Zamknij'),
          ),
        ],
      );
    },
  );
}
