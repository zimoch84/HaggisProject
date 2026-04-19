import 'package:flutter/material.dart';

import '../../../view_models/game_view_model.dart';

const String _miniCardBackAssetPath = 'assets/Cards/OldStyle/rewers_mini.png';

class TopRibbon extends StatelessWidget {
  const TopRibbon({
    super.key,
    required this.viewModel,
    required this.ownPlayer,
    required this.opponents,
    required this.onLeave,
    required this.hasLastRound,
    required this.onOpenLastRound,
    required this.onOpenScoreHistory,
    required this.onRefresh,
    required this.onOpenHandTuning,
  });

  final GameViewModel viewModel;
  final GamePlayerViewModel? ownPlayer;
  final List<GamePlayerViewModel> opponents;
  final VoidCallback onLeave;
  final bool hasLastRound;
  final Future<void> Function() onOpenLastRound;
  final Future<void> Function() onOpenScoreHistory;
  final VoidCallback onRefresh;
  final Future<void> Function() onOpenHandTuning;

  @override
  Widget build(BuildContext context) {
    final playerCount = viewModel.players.length;
    final items = <Widget>[
      InfoChip(icon: Icons.meeting_room_outlined, label: viewModel.roomName),
      InfoChip(icon: Icons.group_outlined, label: '$playerCount'),
      InfoChip(icon: Icons.casino_outlined, label: 'R${viewModel.roundNumber}'),
      if (ownPlayer != null)
        PlayerPill(player: ownPlayer!, isSelf: true, compact: true),
      ...opponents.map(
        (GamePlayerViewModel player) =>
            PlayerPill(player: player, isSelf: false, compact: true),
      ),
    ];

    return Row(
      children: [
        IconButton(
          onPressed: onLeave,
          icon: const Icon(Icons.arrow_back),
          color: Colors.white,
        ),
        Flexible(
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                for (int index = 0; index < items.length; index++) ...[
                  if (index > 0) const SizedBox(width: 8),
                  items[index],
                ],
              ],
            ),
          ),
        ),
        const SizedBox(width: 8),
        IconButton(
          tooltip: 'Last round',
          onPressed: hasLastRound ? onOpenLastRound : null,
          icon: const Icon(Icons.flag_outlined),
          color: Colors.white,
        ),
        IconButton(
          tooltip: 'Score history',
          onPressed: viewModel.scoreHistoryAvailable
              ? onOpenScoreHistory
              : null,
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
      ],
    );
  }
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
  });

  final GamePlayerViewModel player;
  final bool isSelf;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final initial = player.id.isEmpty
        ? '?'
        : player.id.substring(0, 1).toUpperCase();

    return Container(
      padding: EdgeInsets.symmetric(
        horizontal: compact ? 10 : 12,
        vertical: compact ? 8 : 10,
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
              Text(
                player.id,
                style: const TextStyle(
                  fontWeight: FontWeight.w800,
                  color: Colors.white,
                  fontSize: 12,
                ),
              ),
              if (compact)
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      '${player.score} pkt',
                      style: const TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w700,
                        color: Colors.white70,
                      ),
                    ),
                    const SizedBox(width: 6),
                    _HaggisFaceMarkers(player: player),
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
          width: 20,
          height: 28,
          fit: BoxFit.contain,
        ),
        Positioned(
          bottom: -3,
          right: -8,
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
          const Text(
            'finish',
            style: TextStyle(
              fontSize: 10,
              fontWeight: FontWeight.w700,
              color: Color(0xFF9BE2BF),
            ),
          ),
        ],
      ],
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
          fontSize: 10,
          fontWeight: FontWeight.w800,
          color: isActive ? Colors.white : Colors.white54,
          height: 1,
        ),
      ),
    );
  }
}
