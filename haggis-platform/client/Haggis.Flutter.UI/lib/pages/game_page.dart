import 'package:flutter/material.dart';
import '../controllers/game_controller.dart';
import 'game/game_page_actions.dart';
import 'game/game_page_view.dart';
import 'game/models/card_ui_models.dart';

class GamePage extends StatefulWidget {
  const GamePage({super.key, required this.controller, required this.onLeave});
  final GameController controller;
  final VoidCallback onLeave;
  @override
  State<GamePage> createState() => _GamePageState();
}

class _GamePageState extends State<GamePage> {
  HandSortMode handSortMode = HandSortMode.rank;
  double handCardScale = 1.44;
  double handSpacingScale = 1.17;
  double handArcScale = 1.25;
  double handVerticalOffset = 48.49;
  bool showCardHitZones = false;
  final List<String> savedGroup = <String>[];
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
    return buildGamePageView(
      context: context,
      controller: widget.controller,
      onLeave: widget.onLeave,
      handSortMode: handSortMode,
      onSortChanged: (HandSortMode mode) {
        setState(() {
          handSortMode = mode;
        });
      },
      savedGroup: savedGroup,
      onCreateGroup: _createGroupFromSelection,
      onClearGroup: _clearSavedGroup,
      onGroupRainbowBomb: _findBombGroup(_BombGroupType.rainbow) == null
          ? null
          : () => _createBombGroup(_BombGroupType.rainbow),
      onGroupColorBomb: _findBombGroup(_BombGroupType.color) == null
          ? null
          : () => _createBombGroup(_BombGroupType.color),
      showCardHitZones: showCardHitZones,
      handCardScale: handCardScale,
      handSpacingScale: handSpacingScale,
      handArcScale: handArcScale,
      handVerticalOffset: handVerticalOffset,
      onOpenLastRound: _openLastRound,
      onOpenScoreHistory: _openScoreHistory,
      onOpenHandTuning: _openHandTuning,
      onPlaySelected: _handlePlaySelected,
      onCardTap: _handleCardTap,
    );
  }

  Future<void> _openScoreHistory() =>
      openScoreHistory(context: context, controller: widget.controller);
  Future<void> _openLastRound() =>
      openLastRound(context: context, controller: widget.controller);
  Future<void> _openHandTuning() => openHandTuning(
    context: context,
    handCardScale: handCardScale,
    handSpacingScale: handSpacingScale,
    handArcScale: handArcScale,
    handVerticalOffset: handVerticalOffset,
    showCardHitZones: showCardHitZones,
    onCardScaleChanged: (double value) {
      setState(() {
        handCardScale = value;
      });
    },
    onSpacingScaleChanged: (double value) {
      setState(() {
        handSpacingScale = value;
      });
    },
    onArcScaleChanged: (double value) {
      setState(() {
        handArcScale = value;
      });
    },
    onVerticalOffsetChanged: (double value) {
      setState(() {
        handVerticalOffset = value;
      });
    },
    onShowCardHitZonesChanged: (bool value) {
      setState(() {
        showCardHitZones = value;
      });
    },
  );
  Future<void> _handlePlaySelected() async {
    await handlePlaySelected(context: context, controller: widget.controller);
  }

  Future<void> _handleCardTap(String card) => handleCardTap(
    context: context,
    controller: widget.controller,
    card: card,
  );

  void _createGroupFromSelection() {
    final selectedCards = widget.controller.selectedCards;
    if (selectedCards.isEmpty) {
      return;
    }

    setState(() {
      savedGroup
        ..clear()
        ..addAll(selectedCards);
    });
    widget.controller.clearSelectedCards();
  }

  void _clearSavedGroup() {
    if (savedGroup.isEmpty) {
      return;
    }

    setState(() {
      savedGroup.clear();
    });
  }

  void _createBombGroup(_BombGroupType type) {
    final bombCards = _findBombGroup(type);
    if (bombCards == null) {
      return;
    }

    setState(() {
      savedGroup
        ..clear()
        ..addAll(bombCards);
    });
    widget.controller.clearSelectedCards();
  }

  List<String>? _findBombGroup(_BombGroupType type) {
    final hand = widget.controller.viewModel.hand;
    final cardsByRank = <int, List<_BombCandidateCard>>{
      3: <_BombCandidateCard>[],
      5: <_BombCandidateCard>[],
      7: <_BombCandidateCard>[],
      9: <_BombCandidateCard>[],
    };

    for (var index = 0; index < hand.length; index++) {
      final parsed = _BombCandidateCard.tryParse(hand[index], index);
      if (parsed == null || !cardsByRank.containsKey(parsed.rank)) {
        continue;
      }

      cardsByRank[parsed.rank]!.add(parsed);
    }

    if (cardsByRank.values.any(
      (List<_BombCandidateCard> cards) => cards.isEmpty,
    )) {
      return null;
    }

    for (final three in cardsByRank[3]!) {
      for (final five in cardsByRank[5]!) {
        for (final seven in cardsByRank[7]!) {
          for (final nine in cardsByRank[9]!) {
            final bomb = <_BombCandidateCard>[three, five, seven, nine];
            if (_matchesBombType(bomb, type)) {
              bomb.sort(
                (_BombCandidateCard left, _BombCandidateCard right) =>
                    left.index.compareTo(right.index),
              );
              return bomb.map((_BombCandidateCard card) => card.label).toList();
            }
          }
        }
      }
    }

    return null;
  }

  bool _matchesBombType(List<_BombCandidateCard> cards, _BombGroupType type) {
    final suits = cards.map((_BombCandidateCard card) => card.suit).toSet();
    return switch (type) {
      _BombGroupType.rainbow => suits.length == cards.length,
      _BombGroupType.color => suits.length == 1,
    };
  }

  void _syncSavedGroupWithHand() {
    if (savedGroup.isEmpty) {
      return;
    }

    final hand = widget.controller.viewModel.hand;
    savedGroup.removeWhere((String card) => !hand.contains(card));
  }

  void _refresh() {
    _syncSavedGroupWithHand();
    handleRefresh(
      mounted: mounted,
      context: context,
      controller: widget.controller,
      onRebuild: () => setState(() {}),
    );
  }
}

enum _BombGroupType { rainbow, color }

class _BombCandidateCard {
  const _BombCandidateCard({
    required this.label,
    required this.rank,
    required this.suit,
    required this.index,
  });

  final String label;
  final int rank;
  final String suit;
  final int index;

  static _BombCandidateCard? tryParse(String label, int index) {
    final normalized = label.trim().toUpperCase();
    final match = RegExp(r'^(3|5|7|9)([BGROY])$').firstMatch(normalized);
    if (match == null) {
      return null;
    }

    return _BombCandidateCard(
      label: label,
      rank: int.parse(match.group(1)!),
      suit: match.group(2)!,
      index: index,
    );
  }
}
