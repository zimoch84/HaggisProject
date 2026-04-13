class RoundOverViewModel {
  const RoundOverViewModel({
    required this.gameId,
    required this.roundNumber,
    required this.nextRoundNumber,
    required this.status,
    required this.winnerPlayerId,
    required this.players,
    required this.haggisCards,
    required this.lastSequenceLines,
  });

  final String gameId;
  final int roundNumber;
  final int nextRoundNumber;
  final String status;
  final String winnerPlayerId;
  final List<RoundOverPlayerRowViewModel> players;
  final List<String> haggisCards;
  final List<String> lastSequenceLines;
}

class RoundOverPlayerRowViewModel {
  const RoundOverPlayerRowViewModel({
    required this.playerId,
    required this.tricksPoints,
    required this.opponentsRemainingCardsPoints,
    required this.haggisPoints,
    required this.roundPoints,
    required this.totalPoints,
  });

  final String playerId;
  final int tricksPoints;
  final int opponentsRemainingCardsPoints;
  final int haggisPoints;
  final int roundPoints;
  final int totalPoints;
}
