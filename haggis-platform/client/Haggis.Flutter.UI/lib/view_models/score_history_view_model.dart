class ScoreHistoryViewModel {
  const ScoreHistoryViewModel({
    required this.gameId,
    required this.roundNumbers,
    required this.players,
  });

  final String gameId;
  final List<int> roundNumbers;
  final List<ScoreHistoryPlayerRowViewModel> players;
}

class ScoreHistoryPlayerRowViewModel {
  const ScoreHistoryPlayerRowViewModel({
    required this.playerId,
    required this.roundPoints,
    required this.totalPoints,
  });

  final String playerId;
  final List<int> roundPoints;
  final int totalPoints;
}
