import 'package:flutter/material.dart';

class GameViewModel {
  const GameViewModel({
    required this.gameId,
    required this.roomName,
    required this.playerId,
    required this.status,
    required this.version,
    required this.roundNumber,
    required this.currentPlayerId,
    required this.canStartGame,
    required this.isCurrentPlayersTurn,
    required this.isGameInitialized,
    required this.hand,
    required this.players,
    required this.trick,
    required this.possibleActions,
    required this.previousRoundSummary,
    required this.scoreHistoryAvailable,
  });

  final String gameId;
  final String roomName;
  final String playerId;
  final String status;
  final int version;
  final int roundNumber;
  final String currentPlayerId;
  final bool canStartGame;
  final bool isCurrentPlayersTurn;
  final bool isGameInitialized;
  final List<String> hand;
  final List<GamePlayerViewModel> players;
  final List<TrickMoveViewModel> trick;
  final List<PossibleActionViewModel> possibleActions;
  final RoundSummaryBadgeViewModel? previousRoundSummary;
  final bool scoreHistoryAvailable;
}

class GamePlayerViewModel {
  const GamePlayerViewModel({
    required this.id,
    required this.score,
    required this.handCount,
    required this.finished,
    required this.isCurrentPlayer,
  });

  final String id;
  final int score;
  final int handCount;
  final bool finished;
  final bool isCurrentPlayer;
}

class TrickMoveViewModel {
  const TrickMoveViewModel({
    required this.playerId,
    required this.description,
  });

  final String playerId;
  final String description;
}

class PossibleActionViewModel {
  const PossibleActionViewModel({
    required this.type,
    required this.displayAction,
    required this.accentColor,
  });

  final String type;
  final String displayAction;
  final Color accentColor;
}

class RoundSummaryBadgeViewModel {
  const RoundSummaryBadgeViewModel({
    required this.roundNumber,
    required this.winnerPlayerId,
    required this.haggisCards,
  });

  final int roundNumber;
  final String winnerPlayerId;
  final List<String> haggisCards;
}
