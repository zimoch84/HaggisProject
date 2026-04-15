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
  Future<void> _handlePlaySelected() =>
      handlePlaySelected(context: context, controller: widget.controller);
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
  }

  void _clearSavedGroup() {
    if (savedGroup.isEmpty) {
      return;
    }

    setState(() {
      savedGroup.clear();
    });
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
