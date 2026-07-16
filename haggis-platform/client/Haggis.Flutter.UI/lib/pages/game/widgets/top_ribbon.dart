import 'package:flutter/material.dart';

import '../../../view_models/game_view_model.dart';
import '../utils/card_ui_helpers.dart';

const String _miniCardBackAssetPath = 'assets/Cards/OldStyle/rewers_mini.png';

class TopRibbon extends StatelessWidget {
  const TopRibbon({
    super.key,
    required this.viewModel,
    required this.players,
    required this.onLeave,
    required this.hasLastRound,
    required this.onOpenLastRound,
    required this.onOpenScoreHistory,
    required this.onRefresh,
    required this.onOpenHandTuning,
  });

  final GameViewModel viewModel;
  final List<GamePlayerViewModel> players;
  final VoidCallback onLeave;
  final bool hasLastRound;
  final Future<void> Function() onOpenLastRound;
  final Future<void> Function() onOpenScoreHistory;
  final VoidCallback onRefresh;
  final Future<void> Function() onOpenHandTuning;

  @override
  Widget build(BuildContext context) {
    final playerCount = viewModel.players.length;
    final roomLabel = viewModel.roomName.trim().isNotEmpty
        ? viewModel.roomName.trim()
        : viewModel.gameId;
    final infoItems = <Widget>[
      if (!viewModel.singlePlayer)
        InfoChip(icon: Icons.meeting_room_outlined, label: 'Room: $roomLabel'),
      InfoChip(icon: Icons.group_outlined, label: '$playerCount'),
      InfoChip(icon: Icons.casino_outlined, label: 'R${viewModel.roundNumber}'),
    ];
    final lastMoveByPlayer = <String, PlayerLastMove>{};
    for (final TrickMoveViewModel move in viewModel.currentTrick) {
      final cards = extractCardLabels(move.description);
      if (cards.isNotEmpty) {
        lastMoveByPlayer[move.playerId] = PlayerLastMove(cards: cards);
      } else if (isPassMoveDescription(move.description)) {
        lastMoveByPlayer[move.playerId] = const PlayerLastMove(isPass: true);
      }
    }
    final playerItems = <Widget>[
      ...players.map(
        (GamePlayerViewModel player) => PlayerPill(
          player: player,
          isSelf: player.id == viewModel.playerId,
          compact: true,
          lastMove: lastMoveByPlayer[player.id],
        ),
      ),
    ];

    final actions = <Widget>[
      IconButton(
        tooltip: 'Last round',
        onPressed: hasLastRound ? onOpenLastRound : null,
        icon: const Icon(Icons.flag_outlined),
        color: Colors.white,
      ),
      IconButton(
        tooltip: 'Score history',
        onPressed: viewModel.scoreHistoryAvailable ? onOpenScoreHistory : null,
        icon: const Icon(Icons.scoreboard_outlined),
        color: Colors.white,
      ),
      IconButton(
        tooltip: 'Refresh',
        onPressed: onRefresh,
        icon: const Icon(Icons.sync),
        color: Colors.white,
      ),
      IconButton(
        tooltip: 'Hand tuning',
        onPressed: onOpenHandTuning,
        icon: const Icon(Icons.tune),
        color: Colors.white,
      ),
    ];

    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        final useSingleLine = constraints.maxWidth >= 720;
        if (useSingleLine) {
          return SizedBox(
            height: 72,
            child: Row(
              children: [
                IconButton(
                  onPressed: onLeave,
                  icon: const Icon(Icons.arrow_back),
                  color: Colors.white,
                ),
                Expanded(
                  child: ClipRect(
                    child: SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: Row(
                        children: [
                          for (final item in [
                            ...infoItems,
                            ...playerItems,
                          ]) ...[item, const SizedBox(width: 8)],
                        ],
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Row(mainAxisSize: MainAxisSize.min, children: actions),
              ],
            ),
          );
        }

        return Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                IconButton(
                  onPressed: onLeave,
                  icon: const Icon(Icons.arrow_back),
                  color: Colors.white,
                ),
                Expanded(
                  child: ClipRect(
                    child: SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: Row(
                        children: [
                          for (
                            int index = 0;
                            index < infoItems.length;
                            index++
                          ) ...[
                            if (index > 0) const SizedBox(width: 8),
                            infoItems[index],
                          ],
                        ],
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Row(mainAxisSize: MainAxisSize.min, children: actions),
              ],
            ),
            if (playerItems.isNotEmpty) ...[
              const SizedBox(height: 6),
              SizedBox(
                height: 72,
                child: ClipRect(
                  child: SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: [
                        for (
                          int index = 0;
                          index < playerItems.length;
                          index++
                        ) ...[
                          if (index > 0) const SizedBox(width: 8),
                          playerItems[index],
                        ],
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ],
        );
      },
    );
  }
}

class PlayerLastMove {
  const PlayerLastMove({this.cards = const <String>[], this.isPass = false});

  final List<String> cards;
  final bool isPass;
}

class InfoChip extends StatelessWidget {
  const InfoChip({super.key, required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: const Color(0xCC162A2E),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: const Color(0x6656B891)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: const Color(0xFF9BE2BF)),
          const SizedBox(width: 6),
          Text(
            label,
            style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              color: Colors.white,
            ),
          ),
        ],
      ),
    );
  }
}

class PlayerPill extends StatelessWidget {
  const PlayerPill({
    super.key,
    required this.player,
    required this.isSelf,
    this.compact = false,
    this.lastMove,
  });

  final GamePlayerViewModel player;
  final bool isSelf;
  final bool compact;
  final PlayerLastMove? lastMove;

  @override
  Widget build(BuildContext context) {
    final initial = player.id.isEmpty
        ? '?'
        : player.id.substring(0, 1).toUpperCase();

    return Container(
      padding: EdgeInsets.symmetric(
        horizontal: compact ? 10 : 12,
        vertical: compact ? 5 : 10,
      ),
      decoration: BoxDecoration(
        color: const Color(0xCC163034),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(
          color: player.isCurrentPlayer
              ? const Color(0xFF5AAE7A)
              : const Color(0x6656B891),
          width: player.isCurrentPlayer ? 1.5 : 1,
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (!compact) ...[
            Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                color: isSelf
                    ? const Color(0xFF2D6A4F)
                    : const Color(0xFF31544B),
                shape: BoxShape.circle,
              ),
              alignment: Alignment.center,
              child: Text(
                initial,
                style: const TextStyle(
                  fontWeight: FontWeight.w900,
                  color: Colors.white,
                ),
              ),
            ),
            const SizedBox(width: 10),
          ] else ...[
            _HandCountBadge(handCount: player.handCount),
            const SizedBox(width: 8),
          ],
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              ConstrainedBox(
                constraints: BoxConstraints(maxWidth: compact ? 72 : 140),
                child: Text(
                  player.id,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontWeight: FontWeight.w800,
                    color: Colors.white,
                    fontSize: 12,
                    height: 1.05,
                  ),
                ),
              ),
              if (compact)
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          '${player.score} pkt',
                          style: const TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.w700,
                            color: Colors.white70,
                            height: 1.05,
                          ),
                        ),
                        const SizedBox(width: 6),
                        _HaggisFaceMarkers(player: player),
                      ],
                    ),
                    if ((lastMove?.cards ?? const <String>[]).isNotEmpty) ...[
                      const SizedBox(height: 3),
                      _MiniMoveCards(cards: lastMove!.cards),
                    ] else if (lastMove?.isPass == true) ...[
                      const SizedBox(height: 3),
                      const _MiniPassMove(),
                    ] else
                      const SizedBox(height: 17),
                  ],
                )
              else
                Text(
                  'score ${player.score}  reka ${player.handCount}${player.finished ? '  finish' : ''}',
                  style: const TextStyle(fontSize: 12, color: Colors.white70),
                ),
            ],
          ),
        ],
      ),
    );
  }
}

class _MiniPassMove extends StatelessWidget {
  const _MiniPassMove();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 18,
      padding: const EdgeInsets.symmetric(horizontal: 6),
      decoration: BoxDecoration(
        color: const Color(0xCC162A2E),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: const Color(0x9956B891), width: 0.8),
      ),
      child: const Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.front_hand_outlined, size: 12, color: Colors.white),
          SizedBox(width: 3),
          Text(
            'Pass',
            style: TextStyle(
              color: Colors.white,
              fontSize: 9,
              fontWeight: FontWeight.w800,
              height: 1,
            ),
          ),
        ],
      ),
    );
  }
}

class _MiniMoveCards extends StatelessWidget {
  const _MiniMoveCards({required this.cards});

  final List<String> cards;

  @override
  Widget build(BuildContext context) {
    final visibleCards = cards.take(5).toList(growable: false);
    const cardWidth = 16.0;
    const step = 10.0;
    final width = visibleCards.isEmpty
        ? 0.0
        : cardWidth +
              (visibleCards.length - 1) * step +
              (cards.length > 5 ? 18 : 0);
    return SizedBox(
      width: width,
      height: 18,
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          for (var index = 0; index < visibleCards.length; index++)
            Positioned(
              left: index * step,
              top: 0,
              child: _MiniMoveCard(label: visibleCards[index]),
            ),
          if (cards.length > visibleCards.length)
            Positioned(
              left: visibleCards.length * step + 2,
              top: 1,
              child: Text(
                '+${cards.length - visibleCards.length}',
                style: const TextStyle(
                  color: Colors.white70,
                  fontSize: 9,
                  fontWeight: FontWeight.w800,
                  height: 1,
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _MiniMoveCard extends StatelessWidget {
  const _MiniMoveCard({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    final normalized = label.trim().toUpperCase();
    final suit = normalized.isNotEmpty
        ? normalized.substring(normalized.length - 1)
        : '';
    final isWild = RegExp(r'^[JQK]').hasMatch(normalized);
    final display = normalized.replaceAll(RegExp(r'\[[^\]]+\]'), '');
    return Container(
      width: 16,
      height: 18,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: isWild ? const Color(0xFFEEE3CC) : cardAccent(suit),
        borderRadius: BorderRadius.circular(3),
        border: Border.all(color: const Color(0xFFD8CFBD), width: 0.7),
      ),
      child: Text(
        display,
        maxLines: 1,
        style: TextStyle(
          color: isWild || suit == 'Y' ? const Color(0xFF162A2E) : Colors.white,
          fontSize: display.length > 1 ? 7 : 8,
          fontWeight: FontWeight.w900,
          height: 1,
        ),
      ),
    );
  }
}

class _HandCountBadge extends StatelessWidget {
  const _HandCountBadge({required this.handCount});

  final int handCount;

  @override
  Widget build(BuildContext context) {
    return Stack(
      clipBehavior: Clip.none,
      alignment: Alignment.center,
      children: [
        Image.asset(
          _miniCardBackAssetPath,
          width: 18,
          height: 24,
          fit: BoxFit.contain,
        ),
        Positioned(
          bottom: 0,
          right: -6,
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
            decoration: BoxDecoration(
              color: const Color(0xFFB15D45),
              borderRadius: BorderRadius.circular(999),
              border: Border.all(color: const Color(0xFFD8CFBD), width: 0.8),
            ),
            child: Text(
              '$handCount',
              style: const TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w800,
                color: Color.fromARGB(255, 255, 255, 255),
                height: 1,
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _HaggisFaceMarkers extends StatelessWidget {
  const _HaggisFaceMarkers({required this.player});

  final GamePlayerViewModel player;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        _FaceMarker(label: 'J', isActive: player.hasJack),
        const SizedBox(width: 3),
        _FaceMarker(label: 'Q', isActive: player.hasQueen),
        const SizedBox(width: 3),
        _FaceMarker(label: 'K', isActive: player.hasKing),
        if (player.finished) ...[
          const SizedBox(width: 6),
          _FinishPositionMarker(position: player.finishPosition),
        ],
      ],
    );
  }
}

class _FinishPositionMarker extends StatelessWidget {
  const _FinishPositionMarker({required this.position});

  final int position;

  @override
  Widget build(BuildContext context) {
    final label = position > 0 ? '$position' : 'F';
    return Container(
      width: 19,
      height: 19,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: const Color(0xFF9BE2BF),
        shape: BoxShape.circle,
        border: Border.all(color: Colors.white, width: 1.2),
        boxShadow: const [
          BoxShadow(
            color: Color(0x66000000),
            blurRadius: 5,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: Color(0xFF162A2E),
          fontSize: 11,
          fontWeight: FontWeight.w900,
          height: 1,
        ),
      ),
    );
  }
}

class _FaceMarker extends StatelessWidget {
  const _FaceMarker({required this.label, required this.isActive});

  final String label;
  final bool isActive;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
      decoration: BoxDecoration(
        color: isActive ? const Color(0xFFB15D45) : const Color(0x4431544B),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(
          color: isActive ? const Color(0xFFE7C7A1) : const Color(0x6656B891),
        ),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 9,
          fontWeight: FontWeight.w800,
          color: isActive ? Colors.white : Colors.white54,
          height: 1,
        ),
      ),
    );
  }
}
