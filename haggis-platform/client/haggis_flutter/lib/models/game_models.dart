class GameSnapshot {
  GameSnapshot({
    required this.version,
    required this.roundNumber,
    required this.currentPlayerId,
    required this.players,
    required this.trick,
    required this.possibleActions,
    required this.appliedMoves,
    required this.previousRound,
  });

  factory GameSnapshot.fromJson(Map<String, dynamic> json) {
    final data = json['data'] as Map<String, dynamic>? ?? <String, dynamic>{};
    return GameSnapshot(
      version: (json['version'] as num?)?.toInt() ?? 0,
      roundNumber: (data['roundNumber'] as num?)?.toInt() ?? 0,
      currentPlayerId: (data['currentPlayerId'] ?? '').toString(),
      players: (data['players'] as List<dynamic>? ?? <dynamic>[])
          .map(
            (dynamic item) => GamePlayer.fromJson(item as Map<String, dynamic>),
          )
          .toList(),
      trick: (data['trick'] as List<dynamic>? ?? <dynamic>[])
          .map(
            (dynamic item) => TrickMove.fromJson(item as Map<String, dynamic>),
          )
          .toList(),
      possibleActions: (data['possibleActions'] as List<dynamic>? ?? <dynamic>[])
          .map(
            (dynamic item) =>
                PossibleAction.fromJson(item as Map<String, dynamic>),
          )
          .toList(),
      appliedMoves: (data['appliedMoves'] as List<dynamic>? ?? <dynamic>[])
          .map(
            (dynamic item) => TrickMove.fromJson(item as Map<String, dynamic>),
          )
          .toList(),
      previousRound: data['previousRound'] is Map<String, dynamic>
          ? PreviousRound.fromJson(data['previousRound'] as Map<String, dynamic>)
          : null,
    );
  }

  final int version;
  final int roundNumber;
  final String currentPlayerId;
  final List<GamePlayer> players;
  final List<TrickMove> trick;
  final List<PossibleAction> possibleActions;
  final List<TrickMove> appliedMoves;
  final PreviousRound? previousRound;
}

class GamePlayer {
  GamePlayer({
    required this.id,
    required this.score,
    required this.handCount,
    required this.hand,
    required this.finished,
  });

  factory GamePlayer.fromJson(Map<String, dynamic> json) {
    return GamePlayer(
      id: (json['id'] ?? '').toString(),
      score: (json['score'] as num?)?.toInt() ?? 0,
      handCount: (json['handCount'] as num?)?.toInt() ?? 0,
      hand: (json['hand'] as List<dynamic>? ?? <dynamic>[])
          .map((dynamic item) => item.toString())
          .toList(growable: false),
      finished: json['finished'] == true,
    );
  }

  final String id;
  final int score;
  final int handCount;
  final List<String> hand;
  final bool finished;
}

class TrickMove {
  TrickMove({
    required this.playerId,
    required this.description,
  });

  factory TrickMove.fromJson(Map<String, dynamic> json) {
    return TrickMove(
      playerId: (json['playerId'] ?? '').toString(),
      description: (json['desc'] ?? json['action'] ?? '').toString(),
    );
  }

  final String playerId;
  final String description;
}

class PossibleAction {
  PossibleAction({
    required this.type,
    required this.displayAction,
  });

  factory PossibleAction.fromJson(Map<String, dynamic> json) {
    return PossibleAction(
      type: (json['type'] ?? '').toString(),
      displayAction: (json['action'] ?? json['desc'] ?? '').toString(),
    );
  }

  final String type;
  final String displayAction;
}

class PreviousRound {
  PreviousRound({
    required this.roundNumber,
    required this.winnerPlayerName,
    required this.finishingOrderPlayerNames,
    required this.haggisCards,
    required this.playerScores,
  });

  factory PreviousRound.fromJson(Map<String, dynamic> json) {
    return PreviousRound(
      roundNumber: (json['roundNumber'] as num?)?.toInt() ?? 0,
      winnerPlayerName: (json['winnerPlayerName'] ?? '').toString(),
      finishingOrderPlayerNames:
          (json['finishingOrderPlayerNames'] as List<dynamic>? ?? <dynamic>[])
              .map((dynamic item) => item.toString())
              .toList(),
      haggisCards: (json['haggisCards'] as List<dynamic>? ?? <dynamic>[])
          .map((dynamic item) => item.toString())
          .toList(),
      playerScores: (json['playerScores'] as List<dynamic>? ?? <dynamic>[])
          .map(
            (dynamic item) => PreviousRoundPlayerScore.fromJson(
              item as Map<String, dynamic>,
            ),
          )
          .toList(),
    );
  }

  final int roundNumber;
  final String winnerPlayerName;
  final List<String> finishingOrderPlayerNames;
  final List<String> haggisCards;
  final List<PreviousRoundPlayerScore> playerScores;
}

class PreviousRoundPlayerScore {
  PreviousRoundPlayerScore({
    required this.playerName,
    required this.tricksPoints,
    required this.opponentsRemainingCardsPoints,
    required this.haggisPoints,
    required this.roundPoints,
  });

  factory PreviousRoundPlayerScore.fromJson(Map<String, dynamic> json) {
    return PreviousRoundPlayerScore(
      playerName: (json['playerName'] ?? '').toString(),
      tricksPoints: (json['tricksPoints'] as num?)?.toInt() ?? 0,
      opponentsRemainingCardsPoints:
          (json['opponentsRemainingCardsPoints'] as num?)?.toInt() ?? 0,
      haggisPoints: (json['haggisPoints'] as num?)?.toInt() ?? 0,
      roundPoints: (json['roundPoints'] as num?)?.toInt() ?? 0,
    );
  }

  final String playerName;
  final int tricksPoints;
  final int opponentsRemainingCardsPoints;
  final int haggisPoints;
  final int roundPoints;
}
