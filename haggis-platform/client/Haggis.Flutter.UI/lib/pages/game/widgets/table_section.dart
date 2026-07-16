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
    required this.onCreateGroup,
    required this.readyPreviewLabel,
    required this.hasSavedGroup,
    required this.onClearGroup,
    required this.onGroupRainbowBomb,
    required this.onPlayPressed,
  });

  final GameViewModel viewModel;
  final GameController controller;
  final VoidCallback onCreateGroup;
  final String? readyPreviewLabel;
  final bool hasSavedGroup;
  final VoidCallback onClearGroup;
  final VoidCallback? onGroupRainbowBomb;
  final Future<void> Function() onPlayPressed;

  @override
  Widget build(BuildContext context) {
    final visibleMoves = <_VisibleTrickMove>[];
    for (var index = 0; index < viewModel.trick.length; index++) {
      final move = viewModel.trick[index];
      final visibleMove = _VisibleTrickMove(
        playerId: move.playerId,
        description: move.description,
        cards: extractCardLabels(move.description),
        isPass: isPassMoveDescription(move.description),
        index: index,
      );
      if (visibleMove.cards.isNotEmpty || visibleMove.isPass) {
        visibleMoves.add(visibleMove);
      }
    }
    final latestVisibleMove = visibleMoves.isEmpty ? null : visibleMoves.last;
    final latestCardMove = visibleMoves
        .where((_VisibleTrickMove move) => move.cards.isNotEmpty)
        .lastOrNull;
    final latestPassMove = latestVisibleMove?.isPass == true
        ? latestVisibleMove
        : null;
    final collectingTrick = viewModel.collectingTrick;
    final trickKey = visibleMoves.isEmpty
        ? viewModel.trick
              .map(
                (TrickMoveViewModel move) =>
                    '${move.playerId}:${move.description}',
              )
              .join('|')
        : visibleMoves.map((_VisibleTrickMove move) => move.identity).join('|');
    final roomPlayerCount = controller.room.players.length;
    final canAttemptStart = roomPlayerCount >= 2 && roomPlayerCount <= 3;
    final canPass = controller.isCurrentPlayersTurn && controller.canPass;
    final canPlay =
        controller.isCurrentPlayersTurn && controller.canPlaySelectedCards;

    return Column(
      children: [
        Expanded(
          child: AnimatedSwitcher(
            duration: const Duration(milliseconds: 280),
            switchInCurve: Curves.easeOutCubic,
            switchOutCurve: Curves.easeInCubic,
            transitionBuilder: (Widget child, Animation<double> animation) {
              final offsetAnimation = Tween<Offset>(
                begin: const Offset(0, -0.08),
                end: Offset.zero,
              ).animate(animation);
              return FadeTransition(
                opacity: animation,
                child: SlideTransition(position: offsetAnimation, child: child),
              );
            },
            child: collectingTrick != null
                ? Align(
                    alignment: Alignment.topCenter,
                    key: ValueKey<String>(
                      'collect:${collectingTrick.winnerPlayerId}:${collectingTrick.cards.join('|')}',
                    ),
                    child: _AnimatedCollectTrickPile(collect: collectingTrick),
                  )
                : latestVisibleMove != null
                ? Align(
                    alignment: Alignment.topCenter,
                    key: ValueKey<String>('cards:$trickKey'),
                    child: _TableTrickDisplay(
                      cardMove: latestCardMove,
                      passMove: latestPassMove,
                    ),
                  )
                : viewModel.trick.isEmpty
                ? const SizedBox.shrink(key: ValueKey<String>('empty-table'))
                : ListView.separated(
                    key: ValueKey<String>('text:$trickKey'),
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
        ),
        const SizedBox(height: 8),
        FittedBox(
          fit: BoxFit.scaleDown,
          alignment: Alignment.centerLeft,
          child: SizedBox(
            width: MediaQuery.sizeOf(context).width - 64,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    if (viewModel.isGameInitialized)
                      OutlinedButton.icon(
                        onPressed: canPass ? controller.pass : null,
                        style: OutlinedButton.styleFrom(
                          foregroundColor: Colors.white,
                          side: const BorderSide(color: Color(0x6656B891)),
                          padding: const EdgeInsets.symmetric(
                            horizontal: 14,
                            vertical: 10,
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
                        onPressed: canAttemptStart
                            ? controller.startGame
                            : null,
                        style: FilledButton.styleFrom(
                          backgroundColor: const Color(0xFFB15D45),
                          disabledBackgroundColor: const Color(0xFF5C4944),
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 10,
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
                    if (viewModel.isGameInitialized &&
                        controller.selectedCards.isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.only(right: 10),
                        child: FilledButton.tonal(
                          onPressed: onCreateGroup,
                          style: FilledButton.styleFrom(
                            foregroundColor: Colors.white,
                            backgroundColor: const Color(0xAA31544B),
                            padding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 10,
                            ),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(16),
                            ),
                          ),
                          child: const Text('Group'),
                        ),
                      ),
                    if (viewModel.isGameInitialized &&
                        onGroupRainbowBomb != null)
                      Padding(
                        padding: const EdgeInsets.only(right: 10),
                        child: FilledButton.tonalIcon(
                          onPressed: onGroupRainbowBomb,
                          icon: const Icon(
                            Icons.auto_awesome_rounded,
                            size: 18,
                          ),
                          label: const Text('Rainbow'),
                          style: FilledButton.styleFrom(
                            foregroundColor: Colors.white,
                            backgroundColor: const Color(0xAA31544B),
                            padding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 10,
                            ),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(16),
                            ),
                          ),
                        ),
                      ),
                    if (viewModel.isGameInitialized && hasSavedGroup)
                      Padding(
                        padding: const EdgeInsets.only(right: 10),
                        child: FilledButton.tonal(
                          onPressed: onClearGroup,
                          style: FilledButton.styleFrom(
                            foregroundColor: Colors.white,
                            backgroundColor: const Color(0xAA31544B),
                            padding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 10,
                            ),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(16),
                            ),
                          ),
                          child: const Text('Reset grupy'),
                        ),
                      ),
                    if (viewModel.isGameInitialized)
                      FilledButton.icon(
                        onPressed: canPlay ? onPlayPressed : null,
                        style: FilledButton.styleFrom(
                          backgroundColor: const Color(0xFFB15D45),
                          disabledBackgroundColor: const Color(0xFF5C4944),
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 10,
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
                if (viewModel.isGameInitialized &&
                    readyPreviewLabel != null) ...[
                  const SizedBox(height: 8),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 8,
                    ),
                    decoration: BoxDecoration(
                      color: const Color(0xAA162A2E),
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: const Color(0x6656B891)),
                    ),
                    child: Text(
                      readyPreviewLabel!,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _VisibleTrickMove {
  const _VisibleTrickMove({
    required this.playerId,
    required this.description,
    required this.cards,
    required this.isPass,
    required this.index,
  });

  final String playerId;
  final String description;
  final List<String> cards;
  final bool isPass;
  final int index;

  String get identity => '$index:$playerId:$description';
}

class _TableTrickDisplay extends StatelessWidget {
  const _TableTrickDisplay({required this.cardMove, required this.passMove});

  final _VisibleTrickMove? cardMove;
  final _VisibleTrickMove? passMove;

  @override
  Widget build(BuildContext context) {
    final cardMove = this.cardMove;
    final passMove = this.passMove;

    if (cardMove == null && passMove != null) {
      return _TransientPassMoveBadge(
        key: ValueKey(passMove.identity),
        move: passMove,
      );
    }

    if (cardMove == null) {
      return const SizedBox.shrink();
    }

    return Stack(
      clipBehavior: Clip.none,
      alignment: Alignment.topCenter,
      children: [
        Padding(
          padding: const EdgeInsets.only(top: 14),
          child: _AnimatedTableTrickPile(
            move: cardMove,
            isLatest: passMove == null,
          ),
        ),
        if (passMove != null)
          Positioned(
            top: -22,
            child: _TransientPassMoveBadge(
              key: ValueKey(passMove.identity),
              move: passMove,
            ),
          ),
      ],
    );
  }
}

class _TransientPassMoveBadge extends StatefulWidget {
  const _TransientPassMoveBadge({super.key, required this.move});

  final _VisibleTrickMove move;

  @override
  State<_TransientPassMoveBadge> createState() =>
      _TransientPassMoveBadgeState();
}

class _TransientPassMoveBadgeState extends State<_TransientPassMoveBadge> {
  var _visible = true;

  @override
  void initState() {
    super.initState();
    Future<void>.delayed(const Duration(milliseconds: 950), () {
      if (!mounted) {
        return;
      }
      setState(() => _visible = false);
    });
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedOpacity(
      opacity: _visible ? 1 : 0,
      duration: const Duration(milliseconds: 260),
      curve: Curves.easeInOutCubic,
      child: TweenAnimationBuilder<double>(
        tween: Tween<double>(begin: 0, end: 1),
        duration: const Duration(milliseconds: 360),
        curve: Curves.easeOutBack,
        builder: (BuildContext context, double value, Widget? child) {
          final opacity = value.clamp(0.0, 1.0);
          return Opacity(
            opacity: opacity,
            child: Transform.scale(scale: 0.88 + (0.12 * value), child: child),
          );
        },
        child: _PassMoveBadge(playerId: widget.move.playerId),
      ),
    );
  }
}

class _PassMoveBadge extends StatelessWidget {
  const _PassMoveBadge({required this.playerId});

  final String playerId;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(maxWidth: 190),
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
      decoration: BoxDecoration(
        color: const Color(0xCC163034),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: const Color(0x9956B891), width: 1.2),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.front_hand_outlined, color: Colors.white, size: 26),
          const SizedBox(width: 10),
          Flexible(
            child: Text(
              '$playerId: Pass',
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: Colors.white,
                fontSize: 20,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _AnimatedTableTrickPile extends StatelessWidget {
  const _AnimatedTableTrickPile({required this.move, required this.isLatest});

  final _VisibleTrickMove move;
  final bool isLatest;

  @override
  Widget build(BuildContext context) {
    return TweenAnimationBuilder<double>(
      key: ValueKey<String>(move.identity),
      tween: Tween<double>(begin: 0, end: 1),
      duration: const Duration(milliseconds: 420),
      curve: Curves.easeOutBack,
      builder: (BuildContext context, double value, Widget? child) {
        final opacity = value.clamp(0.0, 1.0);
        final slide = (1 - value) * 22;
        return Opacity(
          opacity: opacity,
          child: Transform.translate(
            offset: Offset(0, slide),
            child: Transform.scale(scale: 0.92 + (0.08 * value), child: child),
          ),
        );
      },
      child: _TableMovePile(move: move, isLatest: isLatest),
    );
  }
}

class _AnimatedCollectTrickPile extends StatelessWidget {
  const _AnimatedCollectTrickPile({required this.collect});

  final TrickCollectViewModel collect;

  @override
  Widget build(BuildContext context) {
    final playerCount = collect.playerCount <= 0 ? 1 : collect.playerCount;
    final normalizedTarget = playerCount <= 1
        ? 0.0
        : ((collect.winnerIndex / (playerCount - 1)) * 2) - 1;
    final screenWidth = MediaQuery.sizeOf(context).width;
    final targetX = normalizedTarget * (screenWidth * 0.28);

    return TweenAnimationBuilder<double>(
      tween: Tween<double>(begin: 0, end: 1),
      duration: const Duration(milliseconds: 850),
      curve: Curves.easeInOutCubic,
      builder: (BuildContext context, double value, Widget? child) {
        final opacity = value < 0.72
            ? 1.0
            : (1 - ((value - 0.72) / 0.28)).clamp(0.0, 1.0);
        return Opacity(
          opacity: opacity,
          child: Transform.translate(
            offset: Offset(targetX * value, -120 * value),
            child: Transform.scale(scale: 1 - (0.42 * value), child: child),
          ),
        );
      },
      child: _CollectTrickPile(collect: collect),
    );
  }
}

class _CollectTrickPile extends StatelessWidget {
  const _CollectTrickPile({required this.collect});

  final TrickCollectViewModel collect;

  @override
  Widget build(BuildContext context) {
    const pileScale = 0.72;
    final visibleCards = collect.cards.take(7).toList(growable: false);
    final hiddenCount = collect.cards.length - visibleCards.length;

    return FittedBox(
      fit: BoxFit.scaleDown,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          _PlayerMoveBadge(playerId: collect.winnerPlayerId, isLatest: true),
          const SizedBox(width: 10),
          SizedBox(
            width: _tablePileWidth(visibleCards, scale: pileScale),
            height: _tablePileHeight(visibleCards, scale: pileScale),
            child: TableTrickPile(
              playerId: collect.winnerPlayerId,
              cards: visibleCards,
              scale: pileScale,
            ),
          ),
          if (hiddenCount > 0) ...[
            const SizedBox(width: 8),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
              decoration: BoxDecoration(
                color: const Color(0xDD162A2E),
                borderRadius: BorderRadius.circular(999),
                border: Border.all(color: const Color(0x6656B891)),
              ),
              child: Text(
                '+$hiddenCount',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 12,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _TableMovePile extends StatelessWidget {
  const _TableMovePile({required this.move, required this.isLatest});

  final _VisibleTrickMove move;
  final bool isLatest;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        const pileScale = 1.16;
        return Row(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            _PlayerMoveBadge(playerId: move.playerId, isLatest: isLatest),
            const SizedBox(width: 10),
            SizedBox(
              width: _tablePileWidth(move.cards, scale: pileScale),
              height: _tablePileHeight(move.cards, scale: pileScale),
              child: TableTrickPile(
                playerId: move.playerId,
                cards: move.cards,
                scale: pileScale,
              ),
            ),
          ],
        );
      },
    );
  }
}

class _PlayerMoveBadge extends StatelessWidget {
  const _PlayerMoveBadge({required this.playerId, required this.isLatest});

  final String playerId;
  final bool isLatest;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(maxWidth: 150),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: isLatest ? const Color(0xFFB15D45) : const Color(0xCC162A2E),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(
          color: isLatest ? const Color(0xFFE7C7A1) : const Color(0x6656B891),
        ),
      ),
      child: Text(
        playerId,
        overflow: TextOverflow.ellipsis,
        textAlign: TextAlign.center,
        style: const TextStyle(
          color: Colors.white,
          fontSize: 12,
          fontWeight: FontWeight.w800,
        ),
      ),
    );
  }
}

class TableTrickPile extends StatelessWidget {
  const TableTrickPile({
    super.key,
    required this.playerId,
    required this.cards,
    this.scale = 1,
  });

  final String playerId;
  final List<String> cards;
  final double scale;

  @override
  Widget build(BuildContext context) {
    final horizontalStep = 64.0 * scale;
    final verticalStep = 4.0 * scale;
    final width = _tablePileWidth(cards, scale: scale);
    final height = _tablePileHeight(cards, scale: scale);

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
                child: Transform.scale(
                  scale: scale,
                  alignment: Alignment.topLeft,
                  child: TableCard(label: cards[index]),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

double _tablePileWidth(List<String> cards, {double scale = 1}) {
  return cards.isEmpty ? 0.0 : (96 + (cards.length - 1) * 64.0) * scale;
}

double _tablePileHeight(List<String> cards, {double scale = 1}) {
  return cards.isEmpty ? 0.0 : (136 + (cards.length - 1) * 4.0) * scale;
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
