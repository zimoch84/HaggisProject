import 'package:flutter/material.dart';
import '../../../controllers/game_controller.dart';
import '../../../view_models/game_view_model.dart';
import '../models/card_ui_models.dart';

Set<String> resolvePlayableCards(
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

({List<String> groupedHand, List<String> remainingHand})
splitHandByGroupedCards({
  required List<String> hand,
  required List<String> groupedCards,
  required HandSortMode sortMode,
}) {
  if (groupedCards.isEmpty) {
    return (
      groupedHand: const <String>[],
      remainingHand: List<String>.from(hand, growable: false),
    );
  }

  final groupedPool = List<String>.from(groupedCards);
  final groupedHand = <String>[];
  final remainingHand = <String>[];

  for (final String card in hand) {
    final groupedIndex = groupedPool.indexOf(card);
    if (groupedIndex >= 0) {
      groupedHand.add(card);
      groupedPool.removeAt(groupedIndex);
    } else {
      remainingHand.add(card);
    }
  }

  return (
    groupedHand: sortCardLabels(groupedHand, sortMode),
    remainingHand: remainingHand,
  );
}

List<String> sortCardLabels(List<String> cards, HandSortMode sortMode) {
  final sorted = List<String>.from(cards);
  sorted.sort(
    (String left, String right) => compareCardLabels(left, right, sortMode),
  );
  return sorted;
}

int compareCardLabels(String left, String right, HandSortMode sortMode) {
  final leftCard = ParsedCardLabel.fromRaw(left);
  final rightCard = ParsedCardLabel.fromRaw(right);

  if (sortMode == HandSortMode.color) {
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

List<String> extractCardLabels(String raw) {
  final matches = RegExp(
    r'[JQK](?:\[[^\]]+\])?|(10|[2-9A])[BGROY]',
    caseSensitive: false,
  ).allMatches(raw.toUpperCase());
  return matches.map((Match match) => match.group(0)!).toList(growable: false);
}

Color cardAccent(String suitToken) {
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

String? cardSuitAssetPath(String suitToken) {
  switch (suitToken.toUpperCase()) {
    case 'B':
      return 'assets/Cards/OldStyle/black_symbol.png';
    case 'G':
      return 'assets/Cards/OldStyle/green_symbol.png';
    case 'R':
      return 'assets/Cards/OldStyle/red_symbol.png';
    case 'O':
      return 'assets/Cards/OldStyle/orange_symbol.png';
    case 'Y':
      return 'assets/Cards/OldStyle/yellow_symbol.png';
    default:
      return null;
  }
}

String? wildMiniAssetPath(String rankToken) {
  switch (rankToken.toUpperCase()) {
    case 'J':
      return 'assets/Cards/OldStyle/jack_mini.png';
    case 'Q':
      return 'assets/Cards/OldStyle/queen_mini.png';
    case 'K':
      return 'assets/Cards/OldStyle/king_mini.png';
    default:
      return null;
  }
}

String? wildCardAssetPath(String rankToken) {
  switch (rankToken.toUpperCase()) {
    case 'J':
      return 'assets/Cards/OldStyle/jack.png';
    case 'Q':
      return 'assets/Cards/OldStyle/queen.png';
    case 'K':
      return 'assets/Cards/OldStyle/king.png';
    default:
      return null;
  }
}
