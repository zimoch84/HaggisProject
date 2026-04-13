import 'package:flutter/material.dart';

import '../controllers/game_controller.dart';
import '../view_models/game_view_model.dart';
import 'round_over_page.dart';
import 'score_history_page.dart';

class GamePage extends StatefulWidget {
  const GamePage({super.key, required this.controller, required this.onLeave});

  final GameController controller;
  final VoidCallback onLeave;

  @override
  State<GamePage> createState() => _GamePageState();
}

class _GamePageState extends State<GamePage> {
  _HandSortMode _handSortMode = _HandSortMode.rank;

  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_refresh);
  }

  @override
  void dispose() {
    widget.controller.removeListener(_refresh);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final viewModel = widget.controller.viewModel;
    final ownPlayer =
        viewModel.players
            .where(
              (GamePlayerViewModel player) => player.id == viewModel.playerId,
            )
            .isEmpty
        ? null
        : viewModel.players.firstWhere(
            (GamePlayerViewModel player) => player.id == viewModel.playerId,
          );
    final opponents = viewModel.players
        .where((GamePlayerViewModel player) => player.id != viewModel.playerId)
        .toList(growable: false);
    final sortedHand = _sortCardLabels(viewModel.hand, _handSortMode);
    final playableCards = _resolvePlayableCards(widget.controller, viewModel);

    return Scaffold(
      extendBodyBehindAppBar: true,
      body: Stack(
        children: [
          const _GameBackground(),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(14, 12, 14, 14),
              child: LayoutBuilder(
                builder: (BuildContext context, BoxConstraints constraints) {
                  final isLandscape =
                      constraints.maxWidth > constraints.maxHeight;
                  if (isLandscape) {
                    return Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _TopRibbon(
                          viewModel: viewModel,
                          ownPlayer: ownPlayer,
                          opponents: opponents,
                          onLeave: widget.onLeave,
                          hasLastRound: widget
                              .controller
                              .roundOverController
                              .hasLastRound,
                          onOpenLastRound: _openLastRound,
                          onOpenScoreHistory: _openScoreHistory,
                          onRefresh: widget.controller.requestSnapshot,
                        ),
                        const SizedBox(height: 12),
                        Expanded(
                          child: _SurfaceCard(
                            padding: const EdgeInsets.fromLTRB(18, 18, 18, 14),
                            backgroundColor: Colors.transparent,
                            borderColor: Colors.transparent,
                            boxShadow: const [],
                            child: _TableSection(
                              viewModel: viewModel,
                              controller: widget.controller,
                              onPlayPressed: _handlePlaySelected,
                            ),
                          ),
                        ),
                        const SizedBox(height: 14),
                        SizedBox(
                          height: 124,
                          child: _HandSection(
                            cards: sortedHand,
                            playableCards: playableCards,
                            selectedCards: widget.controller.selectedCards,
                            cardLabelBuilder:
                                widget.controller.displayCardLabel,
                            onCardTap: _handleCardTap,
                            handSortMode: _handSortMode,
                            onSortChanged: (_HandSortMode mode) {
                              setState(() {
                                _handSortMode = mode;
                              });
                            },
                          ),
                        ),
                      ],
                    );
                  }

                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      _TopRibbon(
                        viewModel: viewModel,
                        ownPlayer: ownPlayer,
                        opponents: opponents,
                        onLeave: widget.onLeave,
                        hasLastRound:
                            widget.controller.roundOverController.hasLastRound,
                        onOpenLastRound: _openLastRound,
                        onOpenScoreHistory: _openScoreHistory,
                        onRefresh: widget.controller.requestSnapshot,
                      ),
                      const SizedBox(height: 12),
                      Expanded(
                        child: _SurfaceCard(
                          padding: const EdgeInsets.fromLTRB(18, 18, 18, 14),
                          backgroundColor: Colors.transparent,
                          borderColor: Colors.transparent,
                          boxShadow: const [],
                          child: _TableSection(
                            viewModel: viewModel,
                            controller: widget.controller,
                            onPlayPressed: _handlePlaySelected,
                          ),
                        ),
                      ),
                      const SizedBox(height: 14),
                      SizedBox(
                        height: 124,
                        child: _HandSection(
                          cards: sortedHand,
                          playableCards: playableCards,
                          selectedCards: widget.controller.selectedCards,
                          cardLabelBuilder: widget.controller.displayCardLabel,
                          onCardTap: _handleCardTap,
                          handSortMode: _handSortMode,
                          onSortChanged: (_HandSortMode mode) {
                            setState(() {
                              _handSortMode = mode;
                            });
                          },
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

  Future<void> _openScoreHistory() async {
    final viewModel = widget.controller.scoreHistoryController.viewModel;
    if (viewModel == null) {
      return;
    }

    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (BuildContext context) =>
            ScoreHistoryPage(viewModel: viewModel),
      ),
    );
  }

  Future<void> _openLastRound() async {
    final viewModel = widget.controller.roundOverController.lastRound;
    if (viewModel == null) {
      return;
    }

    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (BuildContext context) => RoundOverPage(viewModel: viewModel),
      ),
    );
  }

  Future<void> _handlePlaySelected() async {
    var matches = widget.controller.matchingPlayableActions;
    if (matches.isEmpty) {
      return;
    }

    if (matches.length == 1) {
      widget.controller.playSelectedCards(matches.first);
      return;
    }
    for (final String card in widget.controller.selectedCards) {
      if (!widget.controller.isWildCard(card)) {
        continue;
      }

      final options = widget.controller.getWildReplacementOptions(card);
      if (options.length > 1 &&
          !widget.controller.wildAssignments.containsKey(card)) {
        await _showWildAssignmentPopup(card);
        return;
      }
    }

    matches = widget.controller.matchingPlayableActions;
    if (matches.length == 1) {
      widget.controller.playSelectedCards(matches.first);
    }
  }

  Future<void> _handleCardTap(String card) async {
    final controller = widget.controller;
    final wasSelected = controller.isCardSelected(card);

    if (wasSelected) {
      if (controller.isWildCard(card)) {
        final options = controller.getWildReplacementOptions(card);
        if (options.length > 1) {
          await _showWildAssignmentPopup(card);
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
        await _showWildAssignmentPopup(card);
      }
    }
  }

  Future<void> _showWildAssignmentPopup(String wildCard) async {
    final options = widget.controller.getWildReplacementOptions(wildCard);
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
            if (widget.controller.isCardSelected(wildCard))
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
      widget.controller.toggleSelectedCard(wildCard);
      return;
    }

    if (chosen != null && chosen.isNotEmpty) {
      widget.controller.setWildReplacement(wildCard, chosen);
    }
  }

  void _refresh() {
    if (!mounted) {
      return;
    }

    final pendingRound = widget.controller.roundOverController
        .consumePendingRound();
    setState(() {});
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

  static String _statusLabel(GameViewModel viewModel) {
    if (!viewModel.isGameInitialized) {
      return 'Czekam na start';
    }
    if (viewModel.isCurrentPlayersTurn) {
      return 'Twoj ruch';
    }
    return 'Gra trwa';
  }

  static Set<String> _resolvePlayableCards(
    GameController controller,
    GameViewModel viewModel,
  ) {
    final tokens = <String>{};
    for (final String card in viewModel.hand) {
      if (controller.canSelectCard(card)) {
        tokens.add(card);
      }
    }
    return tokens;
  }
}

enum _HandSortMode { rank, color }

class _TopRibbon extends StatelessWidget {
  const _TopRibbon({
    required this.viewModel,
    required this.ownPlayer,
    required this.opponents,
    required this.onLeave,
    required this.hasLastRound,
    required this.onOpenLastRound,
    required this.onOpenScoreHistory,
    required this.onRefresh,
  });

  final GameViewModel viewModel;
  final GamePlayerViewModel? ownPlayer;
  final List<GamePlayerViewModel> opponents;
  final VoidCallback onLeave;
  final bool hasLastRound;
  final Future<void> Function() onOpenLastRound;
  final Future<void> Function() onOpenScoreHistory;
  final VoidCallback onRefresh;

  @override
  Widget build(BuildContext context) {
    final playerCount = viewModel.players.length;
    final items = <Widget>[
      _InfoChip(icon: Icons.meeting_room_outlined, label: viewModel.roomName),
      _InfoChip(icon: Icons.group_outlined, label: '$playerCount'),
      _InfoChip(
        icon: Icons.casino_outlined,
        label: 'R${viewModel.roundNumber}',
      ),
      _InfoChip(
        icon: Icons.play_circle_outline,
        label: viewModel.currentPlayerId.isEmpty
            ? '-'
            : viewModel.currentPlayerId,
      ),
      _InfoChip(icon: Icons.person_outline, label: viewModel.playerId),
      if (ownPlayer != null)
        _PlayerPill(player: ownPlayer!, isSelf: true, compact: true),
      ...opponents.map(
        (GamePlayerViewModel player) =>
            _PlayerPill(player: player, isSelf: false, compact: true),
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
      ],
    );
  }
}

class _HandSection extends StatelessWidget {
  const _HandSection({
    required this.cards,
    required this.playableCards,
    required this.selectedCards,
    required this.cardLabelBuilder,
    required this.onCardTap,
    required this.handSortMode,
    required this.onSortChanged,
  });

  final List<String> cards;
  final Set<String> playableCards;
  final List<String> selectedCards;
  final String Function(String card) cardLabelBuilder;
  final Future<void> Function(String card) onCardTap;
  final _HandSortMode handSortMode;
  final ValueChanged<_HandSortMode> onSortChanged;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Positioned.fill(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 48, 16, 14),
            child: cards.isEmpty
                ? const Center(
                    child: Text(
                      'Brak kart na rece',
                      style: TextStyle(
                        color: Colors.white70,
                        shadows: [
                          Shadow(color: Color(0xCC000000), blurRadius: 8),
                        ],
                      ),
                    ),
                  )
                : _PlayerHandFan(
                    cards: cards,
                    playableCards: playableCards,
                    selectedCards: selectedCards,
                    cardLabelBuilder: cardLabelBuilder,
                    onCardTap: onCardTap,
                  ),
          ),
        ),
        Positioned(
          top: 0,
          right: 0,
          child: _HandSortToggle(
            handSortMode: handSortMode,
            onSortChanged: onSortChanged,
          ),
        ),
      ],
    );
  }
}

class _HandSortToggle extends StatelessWidget {
  const _HandSortToggle({
    required this.handSortMode,
    required this.onSortChanged,
  });

  final _HandSortMode handSortMode;
  final ValueChanged<_HandSortMode> onSortChanged;

  @override
  Widget build(BuildContext context) {
    return SegmentedButton<_HandSortMode>(
      showSelectedIcon: false,
      style: ButtonStyle(
        backgroundColor: WidgetStateProperty.resolveWith((
          Set<WidgetState> states,
        ) {
          if (states.contains(WidgetState.selected)) {
            return const Color(0xFFB15D45);
          }
          return const Color(0x33162A2E);
        }),
        foregroundColor: WidgetStateProperty.all<Color>(Colors.white),
        side: WidgetStateProperty.all(
          const BorderSide(color: Color(0x6656B891)),
        ),
        visualDensity: VisualDensity.compact,
      ),
      segments: const [
        ButtonSegment<_HandSortMode>(
          value: _HandSortMode.rank,
          label: Text('Starsz.'),
        ),
        ButtonSegment<_HandSortMode>(
          value: _HandSortMode.color,
          label: Text('Kolor'),
        ),
      ],
      selected: <_HandSortMode>{handSortMode},
      onSelectionChanged: (Set<_HandSortMode> selection) {
        if (selection.isEmpty) {
          return;
        }
        onSortChanged(selection.first);
      },
    );
  }
}

class _TableSection extends StatelessWidget {
  const _TableSection({
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
        : _extractCardLabels(lastMove.description);
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
                      child: _TableTrickPile(
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

class _TableTrickPile extends StatelessWidget {
  const _TableTrickPile({required this.playerId, required this.cards});

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
                child: _TableCard(label: cards[index]),
              ),
            ),
        ],
      ),
    );
  }
}

class _GameBackground extends StatelessWidget {
  const _GameBackground();

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Positioned.fill(
          child: Image.asset(
            'assets/table.png',
            fit: BoxFit.cover,
            alignment: Alignment.center,
          ),
        ),
        Positioned.fill(
          child: DecoratedBox(
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [
                  const Color(0x55102124),
                  const Color(0x77102124),
                  const Color(0x99102124),
                ],
              ),
            ),
          ),
        ),
        Positioned.fill(
          child: IgnorePointer(child: CustomPaint(painter: _NoisePainter())),
        ),
      ],
    );
  }
}

class _NoisePainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint();
    const step = 7.0;

    for (double y = 0; y < size.height; y += step) {
      for (double x = 0; x < size.width; x += step) {
        final hash = ((x * 31 + y * 17).round()) % 100;
        if (hash < 6) {
          paint.color = Colors.white.withValues(alpha: 0.03);
          canvas.drawCircle(Offset(x, y), 1, paint);
        }
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class _SurfaceCard extends StatelessWidget {
  const _SurfaceCard({
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

class _GameInfoRibbon extends StatelessWidget {
  const _GameInfoRibbon({
    required this.roomName,
    required this.playerCount,
    required this.roundNumber,
    required this.currentPlayerId,
    required this.playerId,
  });

  final String roomName;
  final int playerCount;
  final int roundNumber;
  final String currentPlayerId;
  final String playerId;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        _InfoChip(icon: Icons.meeting_room_outlined, label: roomName),
        _InfoChip(icon: Icons.group_outlined, label: '$playerCount'),
        _InfoChip(icon: Icons.casino_outlined, label: 'R$roundNumber'),
        _InfoChip(
          icon: Icons.play_circle_outline,
          label: currentPlayerId.isEmpty ? '-' : currentPlayerId,
        ),
        _InfoChip(icon: Icons.person_outline, label: playerId),
      ],
    );
  }
}

class _InfoChip extends StatelessWidget {
  const _InfoChip({required this.icon, required this.label});

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

class _PlayerPill extends StatelessWidget {
  const _PlayerPill({
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
            Icon(
              isSelf ? Icons.person : Icons.person_outline,
              size: 16,
              color: const Color(0xFF9BE2BF),
            ),
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
              if (!compact)
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

class _TableCard extends StatelessWidget {
  const _TableCard({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    final display = _DisplayCardLabel.fromLabel(label);
    final centerToken = display.isWildAssignment
        ? '${display.assignmentRankToken}${display.assignmentSuitToken}'
        : display.suitToken;

    return Container(
      width: 96,
      height: 136,
      decoration: BoxDecoration(
        color: const Color(0xFFF7F1E6),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: const Color(0xFFD8CFBD), width: 1.5),
        boxShadow: const [
          BoxShadow(
            color: Color(0x22000000),
            blurRadius: 6,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Stack(
        children: [
          Positioned(
            top: 10,
            left: 10,
            child: _CardCorner(display: display, compact: false),
          ),
          Center(
            child: Text(
              centerToken,
              style: TextStyle(
                fontSize: display.isWildAssignment ? 24 : 28,
                fontWeight: FontWeight.w800,
                color:
                    (display.accentToken.isEmpty
                            ? const Color(0xFF8A5B1F)
                            : _cardAccent(display.accentToken))
                        .withValues(alpha: 0.22),
              ),
            ),
          ),
          Positioned(
            right: 10,
            bottom: 10,
            child: Transform.rotate(
              angle: 3.14159,
              child: _CardCorner(display: display, compact: false),
            ),
          ),
        ],
      ),
    );
  }
}

class _HandCard extends StatelessWidget {
  const _HandCard({
    required this.label,
    required this.isPlayable,
    required this.isSelected,
    this.onTap,
  });

  final String label;
  final bool isPlayable;
  final bool isSelected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final display = _DisplayCardLabel.fromLabel(label);

    return GestureDetector(
      onTap: onTap,
      child: AnimatedScale(
        scale: isSelected ? 1.03 : 1,
        duration: const Duration(milliseconds: 160),
        curve: Curves.easeOutCubic,
        child: Opacity(
          opacity: isPlayable || isSelected ? 1 : 0.82,
          child: Container(
            width: 64,
            height: 98,
            decoration: BoxDecoration(
              color: const Color(0xFFF6F1E7),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: isSelected
                    ? const Color(0xFFF2C14E)
                    : isPlayable
                    ? const Color(0xFF5AAE7A)
                    : const Color(0xFFD8CFBD),
                width: isSelected || isPlayable ? 2 : 1,
              ),
              boxShadow: [
                BoxShadow(
                  color: isSelected
                      ? const Color(0x44F2C14E)
                      : const Color(0x22000000),
                  blurRadius: isSelected ? 14 : 6,
                  offset: Offset(0, isSelected ? 8 : 3),
                ),
              ],
            ),
            child: Padding(
              padding: const EdgeInsets.all(8),
              child: Align(
                alignment: Alignment.topLeft,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF0E8D8),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: const Color(0x22B4A893)),
                  ),
                  child: _CardCorner(display: display, compact: true),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _CardCorner extends StatelessWidget {
  const _CardCorner({required this.display, required this.compact});

  final _DisplayCardLabel display;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final rankSize = compact ? 16.0 : 22.0;
    final suitSize = compact ? 13.0 : 18.0;
    final color = display.accentToken.isEmpty
        ? const Color(0xFF8A5B1F)
        : _cardAccent(display.accentToken);

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          display.rankToken,
          style: TextStyle(
            color: color,
            fontSize: rankSize,
            fontWeight: FontWeight.w900,
            height: 1,
          ),
        ),
        if (display.isWildAssignment) ...[
          const SizedBox(height: 2),
          Text(
            display.assignmentRankToken,
            style: TextStyle(
              color: color,
              fontSize: suitSize,
              fontWeight: FontWeight.w800,
              height: 1,
            ),
          ),
          if (display.assignmentSuitToken.isNotEmpty)
            Text(
              display.assignmentSuitToken,
              style: TextStyle(
                color: color,
                fontSize: suitSize,
                fontWeight: FontWeight.w800,
                height: 1,
              ),
            ),
        ] else if (display.suitToken.isNotEmpty) ...[
          const SizedBox(height: 2),
          Text(
            display.suitToken,
            style: TextStyle(
              color: color,
              fontSize: suitSize,
              fontWeight: FontWeight.w800,
              height: 1,
            ),
          ),
        ],
      ],
    );
  }
}

class _PlayerHandFan extends StatelessWidget {
  const _PlayerHandFan({
    required this.cards,
    required this.playableCards,
    required this.selectedCards,
    required this.cardLabelBuilder,
    required this.onCardTap,
  });

  final List<String> cards;
  final Set<String> playableCards;
  final List<String> selectedCards;
  final String Function(String card) cardLabelBuilder;
  final Future<void> Function(String card) onCardTap;

  @override
  Widget build(BuildContext context) {
    const cardWidth = 62.0;
    const preferredStep = 32.0;
    const minimumStep = 14.0;

    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        final availableWidth = constraints.maxWidth.isFinite
            ? constraints.maxWidth
            : cardWidth;
        final fittedStep = cards.length <= 1
            ? cardWidth
            : (availableWidth - cardWidth) / (cards.length - 1);
        final effectiveStep = cards.length <= 1
            ? cardWidth
            : fittedStep.clamp(minimumStep, preferredStep).toDouble();
        final contentWidth = cards.isEmpty
            ? cardWidth
            : cardWidth + (cards.length - 1) * effectiveStep;
        final viewportWidth = contentWidth < availableWidth
            ? availableWidth
            : contentWidth;
        final horizontalInset = (viewportWidth - contentWidth) / 2;

        return SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: SizedBox(
            width: viewportWidth,
            height: 118,
            child: Stack(
              clipBehavior: Clip.none,
              children: [
                for (int index = 0; index < cards.length; index++)
                  _positionedHandCard(
                    index: index,
                    count: cards.length,
                    step: effectiveStep,
                    startOffset: horizontalInset,
                    label: cards[index],
                    displayLabel: cardLabelBuilder(cards[index]),
                    isPlayable: playableCards.contains(cards[index]),
                    isSelected: selectedCards.contains(cards[index]),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _positionedHandCard({
    required int index,
    required int count,
    required double step,
    required double startOffset,
    required String label,
    required String displayLabel,
    required bool isPlayable,
    required bool isSelected,
  }) {
    final center = (count - 1) / 2;
    final distanceFromCenter = index - center;
    final normalized = center == 0 ? 0.0 : distanceFromCenter / center;
    final angle = normalized * 0.12;
    final baseTop = 8 + normalized.abs() * 10;
    final top = isSelected ? baseTop - 18 : baseTop;

    return Positioned(
      left: startOffset + index * step,
      top: top,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 160),
        curve: Curves.easeOutCubic,
        child: Transform.rotate(
          angle: angle,
          alignment: Alignment.bottomCenter,
          child: _HandCard(
            label: displayLabel,
            isPlayable: isPlayable,
            isSelected: isSelected,
            onTap: () => onCardTap(label),
          ),
        ),
      ),
    );
  }
}

List<String> _sortCardLabels(List<String> cards, _HandSortMode sortMode) {
  final sorted = List<String>.from(cards);
  sorted.sort(
    (String left, String right) => _compareCardLabels(left, right, sortMode),
  );
  return sorted;
}

int _compareCardLabels(String left, String right, _HandSortMode sortMode) {
  final leftCard = _ParsedCardLabel.fromRaw(left);
  final rightCard = _ParsedCardLabel.fromRaw(right);

  if (sortMode == _HandSortMode.color) {
    final suitComparison = leftCard.suitOrder.compareTo(rightCard.suitOrder);
    if (suitComparison != 0) {
      return suitComparison;
    }

    final rankComparison = leftCard.rankOrder.compareTo(rightCard.rankOrder);
    if (rankComparison != 0) {
      return rankComparison;
    }

    return leftCard.raw.compareTo(rightCard.raw);
  }

  final rankComparison = leftCard.rankOrder.compareTo(rightCard.rankOrder);
  if (rankComparison != 0) {
    return rankComparison;
  }

  final suitComparison = leftCard.suitOrder.compareTo(rightCard.suitOrder);
  if (suitComparison != 0) {
    return suitComparison;
  }

  return leftCard.raw.compareTo(rightCard.raw);
}

List<String> _extractCardLabels(String raw) {
  final matches = RegExp(
    r'[JQK](?:\[[^\]]+\])?|(10|[2-9A])[BGROY]',
    caseSensitive: false,
  ).allMatches(raw.toUpperCase());
  return matches.map((Match match) => match.group(0)!).toList(growable: false);
}

Color _cardAccent(String suitToken) {
  switch (suitToken.toUpperCase()) {
    case 'B':
      return const Color(0xFF242424);
    case 'G':
      return const Color(0xFF2D7D46);
    case 'R':
      return const Color(0xFFB33A34);
    case 'O':
      return const Color(0xFFC57A1E);
    case 'Y':
      return const Color(0xFF9F7A0A);
    default:
      return const Color(0xFF4A4A4A);
  }
}

class _ParsedCardLabel {
  const _ParsedCardLabel({
    required this.raw,
    required this.rankOrder,
    required this.suitOrder,
  });

  final String raw;
  final int rankOrder;
  final int suitOrder;

  factory _ParsedCardLabel.fromRaw(String rawLabel) {
    final raw = rawLabel.trim().toUpperCase();
    if (raw.isEmpty) {
      return const _ParsedCardLabel(raw: '', rankOrder: 999, suitOrder: 999);
    }

    final rankToken = raw.length > 1 ? raw.substring(0, raw.length - 1) : raw;
    final suitToken = raw.length > 1 ? raw.substring(raw.length - 1) : '';

    return _ParsedCardLabel(
      raw: raw,
      rankOrder: _rankOrder(rankToken),
      suitOrder: _suitOrder(suitToken),
    );
  }

  static int _rankOrder(String rankToken) {
    const ranks = {
      '2': 2,
      '3': 3,
      '4': 4,
      '5': 5,
      '6': 6,
      '7': 7,
      '8': 8,
      '9': 9,
      '10': 10,
      'J': 11,
      'Q': 12,
      'K': 13,
      'A': 14,
    };
    return ranks[rankToken] ?? 999;
  }

  static int _suitOrder(String suitToken) {
    const suits = {'B': 0, 'G': 1, 'R': 2, 'O': 3, 'Y': 4};
    return suits[suitToken] ?? 999;
  }
}

class _DisplayCardLabel {
  const _DisplayCardLabel({
    required this.rankToken,
    required this.suitToken,
    required this.accentToken,
    required this.assignmentRankToken,
    required this.assignmentSuitToken,
    required this.isWildAssignment,
  });

  final String rankToken;
  final String suitToken;
  final String accentToken;
  final String assignmentRankToken;
  final String assignmentSuitToken;
  final bool isWildAssignment;

  factory _DisplayCardLabel.fromLabel(String rawLabel) {
    final raw = rawLabel.trim().toUpperCase();
    if (raw.isEmpty) {
      return const _DisplayCardLabel(
        rankToken: '?',
        suitToken: '?',
        accentToken: '?',
        assignmentRankToken: '',
        assignmentSuitToken: '',
        isWildAssignment: false,
      );
    }
    if (raw.contains('[')) {
      final bracketIndex = raw.indexOf('[');
      final assignment = raw.substring(bracketIndex + 1, raw.length - 1);
      final parsedAssignment = _splitRankAndSuit(assignment);
      return _DisplayCardLabel(
        rankToken: raw.substring(0, bracketIndex),
        suitToken: parsedAssignment.$2,
        accentToken: _extractAccentToken(assignment),
        assignmentRankToken: parsedAssignment.$1,
        assignmentSuitToken: parsedAssignment.$2,
        isWildAssignment: true,
      );
    }
    if (raw.length == 1) {
      return _DisplayCardLabel(
        rankToken: raw,
        suitToken: '?',
        accentToken: '?',
        assignmentRankToken: '',
        assignmentSuitToken: '',
        isWildAssignment: false,
      );
    }

    final parsed = _splitRankAndSuit(raw);
    return _DisplayCardLabel(
      rankToken: parsed.$1,
      suitToken: parsed.$2,
      accentToken: parsed.$2,
      assignmentRankToken: '',
      assignmentSuitToken: '',
      isWildAssignment: false,
    );
  }

  static String _extractAccentToken(String assignment) {
    final match = RegExp(r'([BGROY])$').firstMatch(assignment);
    return match?.group(1) ?? '';
  }

  static (String, String) _splitRankAndSuit(String token) {
    final trimmed = token.trim().toUpperCase();
    if (trimmed.isEmpty) {
      return ('?', '?');
    }
    if (trimmed.length == 1) {
      return (trimmed, '?');
    }
    return (
      trimmed.substring(0, trimmed.length - 1),
      trimmed.substring(trimmed.length - 1),
    );
  }
}
