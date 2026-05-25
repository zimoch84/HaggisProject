import 'package:flutter/material.dart';
import '../../../controllers/game_controller.dart';
import '../../../view_models/game_view_model.dart';
import '../utils/card_ui_helpers.dart';
import 'cards.dart';

class TableSection extends StatelessWidget {
  const TableSection({
    super.key,
    required this.viewModel,
    required this.controller,
    required this.onPlayPressed,
  });

  final GameViewModel viewModel;
  final GameController controller;
  final Future<void> Function() onPlayPressed;

  @override
  Widget build(BuildContext context) {
    final lastMove = viewModel.trick.isEmpty ? null : viewModel.trick.last;
    final lastTrickCards = lastMove == null
        ? const <String>[]
        : extractCardLabels(lastMove.description);
    final roomPlayerCount = controller.room.players.length;
    final canAttemptStart = roomPlayerCount >= 2 && roomPlayerCount <= 3;
    final canPass = controller.isCurrentPlayersTurn && controller.canPass;
    final canPlay =
        controller.isCurrentPlayersTurn && controller.canPlaySelectedCards;

    return Column(
      children: [
        Expanded(
          child: lastTrickCards.isNotEmpty
              ? Center(
                  child: SizedBox(
                    width: double.infinity,
                    height: 180,
                    child: Center(
                      child: TableTrickPile(
                        playerId: lastMove!.playerId,
                        cards: lastTrickCards,
                      ),
                    ),
                  ),
                )
              : viewModel.trick.isEmpty
              ? const SizedBox.shrink()
              : ListView.separated(
                  itemCount: viewModel.trick.length,
                  separatorBuilder: (BuildContext context, int index) =>
                      const SizedBox(height: 10),
                  itemBuilder: (BuildContext context, int index) {
                    final move = viewModel.trick[index];
                    return Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 10,
                      ),
                      decoration: BoxDecoration(
                        color: const Color(0xAA131F22),
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(color: const Color(0x6656B891)),
                      ),
                      child: Text(
                        '${move.playerId}: ${move.description}',
                        style: const TextStyle(color: Colors.white),
                      ),
                    );
                  },
                ),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            if (viewModel.isGameInitialized)
              OutlinedButton.icon(
                onPressed: canPass ? controller.pass : null,
                style: OutlinedButton.styleFrom(
                  foregroundColor: Colors.white,
                  side: const BorderSide(color: Color(0x6656B891)),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 12,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                  ),
                ),
                icon: const Icon(Icons.front_hand_outlined),
                label: const Text(
                  'Pass',
                  style: TextStyle(fontWeight: FontWeight.w700),
                ),
              )
            else
              FilledButton.icon(
                onPressed: canAttemptStart ? controller.startGame : null,
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFFB15D45),
                  disabledBackgroundColor: const Color(0xFF5C4944),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 18,
                    vertical: 12,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                  ),
                ),
                icon: const Icon(Icons.rocket_launch_outlined),
                label: const Text(
                  'StartGame',
                  style: TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
            const Spacer(),
            if (viewModel.isGameInitialized)
              FilledButton.icon(
                onPressed: canPlay ? onPlayPressed : null,
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFFB15D45),
                  disabledBackgroundColor: const Color(0xFF5C4944),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 18,
                    vertical: 12,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                  ),
                ),
                icon: const Icon(Icons.play_arrow_rounded),
                label: const Text(
                  'Play',
                  style: TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
          ],
        ),
      ],
    );
  }
}

class TableTrickPile extends StatelessWidget {
  const TableTrickPile({
    super.key,
    required this.playerId,
    required this.cards,
  });

  final String playerId;
  final List<String> cards;

  @override
  Widget build(BuildContext context) {
    const horizontalStep = 64.0;
    const verticalStep = 4.0;
    final width = cards.isEmpty
        ? 0.0
        : 96 + (cards.length - 1) * horizontalStep;
    final height = cards.isEmpty
        ? 0.0
        : 136 + (cards.length - 1) * verticalStep;

    return SizedBox(
      width: width,
      height: height,
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          for (int index = 0; index < cards.length; index++)
            Positioned(
              left: index * horizontalStep,
              top: index * verticalStep,
              child: Transform.rotate(
                angle: (index - (cards.length - 1) / 2) * 0.018,
                child: TableCard(label: cards[index]),
              ),
            ),
        ],
      ),
    );
  }
}

class SurfaceCard extends StatelessWidget {
  const SurfaceCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(16),
    this.backgroundColor = const Color(0xCC162A2E),
    this.borderColor = const Color(0x6656B891),
    this.boxShadow = const [
      BoxShadow(
        color: Color(0x55000000),
        blurRadius: 20,
        offset: Offset(0, 10),
      ),
    ],
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final Color backgroundColor;
  final Color borderColor;
  final List<BoxShadow> boxShadow;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: padding,
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(26),
        border: Border.all(color: borderColor),
        boxShadow: boxShadow,
      ),
      child: child,
    );
  }
}
