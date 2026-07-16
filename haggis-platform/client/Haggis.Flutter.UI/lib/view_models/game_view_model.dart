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
    required this.currentTrick,
    required this.collectingTrick,
    required this.possibleActions,
    required this.previousRoundSummary,
    required this.scoreHistoryAvailable,
    required this.singlePlayer,
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
  final List<TrickMoveViewModel> currentTrick;
  final TrickCollectViewModel? collectingTrick;
  final List<PossibleActionViewModel> possibleActions;
  final RoundSummaryBadgeViewModel? previousRoundSummary;
  final bool scoreHistoryAvailable;
  final bool singlePlayer;
}

class TrickCollectViewModel {
  const TrickCollectViewModel({
    required this.winnerPlayerId,
    required this.winnerIndex,
    required this.playerCount,
    required this.cards,
  });

  final String winnerPlayerId;
  final int winnerIndex;
  final int playerCount;
  final List<String> cards;
}

class GamePlayerViewModel {
  const GamePlayerViewModel({
    required this.id,
    required this.score,
    required this.handCount,
    required this.finished,
    required this.finishPosition,
    required this.isCurrentPlayer,
    required this.hasJack,
    required this.hasQueen,
    required this.hasKing,
  });

  final String id;
  final int score;
  final int handCount;
  final bool finished;
  final int finishPosition;
  final bool isCurrentPlayer;
  final bool hasJack;
  final bool hasQueen;
  final bool hasKing;
}

class TrickMoveViewModel {
  const TrickMoveViewModel({required this.playerId, required this.description});

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
