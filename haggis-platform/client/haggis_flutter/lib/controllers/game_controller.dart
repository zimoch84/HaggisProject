import 'dart:async';

import 'package:flutter/material.dart';

import '../infrastructure/remote/remote_game_websocket_client.dart';
import '../models/game_models.dart';
import '../models/lobby_models.dart';
import '../view_models/game_view_model.dart';
import '../view_models/round_over_view_model.dart';
import '../view_models/score_history_view_model.dart';
import 'round_over_controller.dart';
import 'score_history_controller.dart';

class GameController extends ChangeNotifier {
  static const int _forcedStartSeed = 115826734;

  GameController({
    required this.serverBaseUrl,
    required this.playerId,
    required this.room,
  });

  final String serverBaseUrl;
  final String playerId;
  final LobbyRoom room;

  late final RemoteGameWebSocketClient _client;
  StreamSubscription<Map<String, dynamic>>? _subscription;

  final RoundOverController roundOverController = RoundOverController();
  final ScoreHistoryController scoreHistoryController =
      ScoreHistoryController();

  GameSnapshot? _snapshot;
  String _status = 'Connecting to game...';
  final List<RoundOverViewModel> _completedRounds = <RoundOverViewModel>[];
  final List<String> _selectedCards = <String>[];
  final Map<String, String> _wildAssignments = <String, String>{};
  bool _autoStartRequested = false;
  bool _hasEstablishedSnapshotBaseline = false;

  GameSnapshot? get snapshot => _snapshot;

  String get status => _status;
  List<String> get selectedCards => List<String>.unmodifiable(_selectedCards);
  Map<String, String> get wildAssignments =>
      Map<String, String>.unmodifiable(_wildAssignments);

  bool get canStartGame =>
      room.players.length >= 2 &&
      room.players.length <= 3 &&
      room.players.isNotEmpty &&
      room.players.first == playerId &&
      !isGameInitialized;

  bool get isCurrentPlayersTurn => _snapshot?.currentPlayerId == playerId;

  bool get isGameInitialized =>
      (_snapshot?.version ?? 0) > 0 &&
      (_snapshot?.currentPlayerId ?? '').isNotEmpty;

  bool get canPass => (_snapshot?.possibleActions ?? const <PossibleAction>[])
      .any((PossibleAction action) => action.type.toLowerCase() == 'pass');

  bool get canPlaySelectedCards => matchingPlayableActions.isNotEmpty;

  PossibleActionViewModel? get selectedPlayableAction {
    final matches = matchingPlayableActions;
    return matches.length == 1 ? matches.first : null;
  }

  List<PossibleActionViewModel> get matchingPlayableActions {
    return _findMatchingPlayableActionsForSelection(
      _selectedCards,
      wildAssignments: _wildAssignments,
    );
  }

  bool isTrickInPossibleActions(
    List<String> cards, {
    Map<String, String>? wildAssignments,
  }) {
    return _findMatchingPlayableActionsForSelection(
      cards,
      wildAssignments: wildAssignments ?? _wildAssignments,
    ).isNotEmpty;
  }

  GameViewModel get viewModel {
    final snapshot = _snapshot;
    final currentPlayerId = snapshot?.currentPlayerId ?? '';
    GamePlayer? currentPlayer;
    for (final GamePlayer player in snapshot?.players ?? const <GamePlayer>[]) {
      if (player.id == playerId) {
        currentPlayer = player;
        break;
      }
    }
    return GameViewModel(
      gameId: room.gameId,
      roomName: room.roomName,
      playerId: playerId,
      status: _status,
      version: snapshot?.version ?? 0,
      roundNumber: snapshot?.roundNumber ?? 0,
      currentPlayerId: currentPlayerId,
      canStartGame: canStartGame,
      isCurrentPlayersTurn: isCurrentPlayersTurn,
      isGameInitialized: isGameInitialized,
      hand: List<String>.unmodifiable(currentPlayer?.hand ?? const <String>[]),
      players: (snapshot?.players ?? const <GamePlayer>[])
          .map(
            (GamePlayer player) => GamePlayerViewModel(
              id: player.id,
              score: player.score,
              handCount: player.handCount,
              finished: player.finished,
              isCurrentPlayer: player.id == currentPlayerId,
              hasJack: player.hand.any(
                (String card) => card.trim().toUpperCase() == 'J',
              ),
              hasQueen: player.hand.any(
                (String card) => card.trim().toUpperCase() == 'Q',
              ),
              hasKing: player.hand.any(
                (String card) => card.trim().toUpperCase() == 'K',
              ),
            ),
          )
          .toList(growable: false),
      trick: (snapshot?.trick ?? const <TrickMove>[])
          .map(
            (TrickMove move) => TrickMoveViewModel(
              playerId: move.playerId,
              description: move.description,
            ),
          )
          .toList(growable: false),
      possibleActions: (snapshot?.possibleActions ?? const <PossibleAction>[])
          .map(
            (PossibleAction action) => PossibleActionViewModel(
              type: action.type,
              displayAction: action.displayAction,
              accentColor: _resolveActionColor(action),
            ),
          )
          .toList(growable: false),
      previousRoundSummary: snapshot?.previousRound == null
          ? null
          : RoundSummaryBadgeViewModel(
              roundNumber: snapshot!.previousRound!.roundNumber,
              winnerPlayerId: snapshot.previousRound!.winnerPlayerName,
              haggisCards: List<String>.unmodifiable(
                snapshot.previousRound!.haggisCards,
              ),
            ),
      scoreHistoryAvailable: scoreHistoryController.hasData,
    );
  }

  Future<void> connect() async {
    _client = RemoteGameWebSocketClient(
      serverBaseUrl: serverBaseUrl,
      gameId: room.gameId,
    );
    await _client.connect();
    _subscription = _client.messages.listen(
      _onMessage,
      onError: (Object error, StackTrace _) {
        _status = 'Game socket error: $error';
        notifyListeners();
      },
      onDone: () {
        _status = 'Game socket closed.';
        notifyListeners();
      },
    );
    joinGame();
    requestSnapshot();
  }

  void joinGame() {
    _client.join(playerId);
  }

  void requestSnapshot() {
    _client.requestSnapshot(playerId);
  }

  void startGame() {
    if (!canStartGame) {
      return;
    }

    _autoStartRequested = true;
    _client.createGame(playerId, room.players.length, seed: _forcedStartSeed);
    _status = 'Start command sent. Seed: $_forcedStartSeed';
    notifyListeners();
  }

  void playAction(PossibleActionViewModel action) {
    if (action.type.toLowerCase() == 'pass') {
      _client.sendPass(playerId);
    } else {
      _client.sendPlay(playerId, action.displayAction);
    }
    _status = 'Sent: ${action.displayAction}';
    _selectedCards.clear();
    _wildAssignments.clear();
    notifyListeners();
  }

  void toggleSelectedCard(String card) {
    if (!canSelectCard(card)) {
      return;
    }

    if (_selectedCards.contains(card)) {
      _selectedCards.remove(card);
      _wildAssignments.remove(card);
    } else {
      _selectedCards.add(card);
    }
    _syncWildAssignmentsWithSelection();
    notifyListeners();
  }

  void clearSelectedCards() {
    if (_selectedCards.isEmpty && _wildAssignments.isEmpty) {
      return;
    }

    _selectedCards.clear();
    _wildAssignments.clear();
    notifyListeners();
  }

  bool canSelectCard(String card) {
    final snapshot = _snapshot;
    if (snapshot == null) {
      return false;
    }

    for (final GamePlayer player in snapshot.players) {
      if (player.id != playerId) {
        continue;
      }

      return player.hand.contains(card);
    }

    return false;
  }

  bool isCardPlayable(String card) {
    return isCurrentPlayersTurn && canSelectCard(card);
  }

  bool isCardSelected(String card) => _selectedCards.contains(card);

  bool isWildCard(String card) =>
      RegExp(r'^[JQK]$', caseSensitive: false).hasMatch(card.trim());

  String displayCardLabel(String card) {
    final assignment = _wildAssignments[card];
    if (assignment == null || assignment.isEmpty) {
      return card;
    }
    return '$card[$assignment]';
  }

  List<String> getWildReplacementOptions(String wildCard) {
    if (!isWildCard(wildCard) || !_selectedCards.contains(wildCard)) {
      return const <String>[];
    }

    final options = <String>{};
    for (final PossibleActionViewModel action
        in _matchingPlayableActionsIgnoringWild(wildCard)) {
      final assignment = _extractWildAssignment(action.displayAction, wildCard);
      if (assignment != null && assignment.isNotEmpty) {
        options.add(assignment);
      }
    }

    final result = options.toList()..sort();
    return result;
  }

  void setWildReplacement(String wildCard, String replacement) {
    if (!isWildCard(wildCard) || replacement.trim().isEmpty) {
      return;
    }

    _wildAssignments[wildCard] = replacement.trim().toUpperCase();
    notifyListeners();
  }

  void playSelectedCards([PossibleActionViewModel? chosenAction]) {
    final action = chosenAction ?? selectedPlayableAction;
    if (action == null) {
      return;
    }

    playAction(action);
  }

  void pass() {
    if (!canPass) {
      return;
    }

    final action = PossibleActionViewModel(
      type: 'Pass',
      displayAction: 'PASS',
      accentColor: const Color(0xFF5D6D69),
    );
    playAction(action);
  }

  @override
  void dispose() {
    _subscription?.cancel();
    _client.dispose();
    roundOverController.dispose();
    scoreHistoryController.dispose();
    super.dispose();
  }

  void _onMessage(Map<String, dynamic> json) {
    final type = (json['type'] ?? '').toString();

    if (type == 'RoomJoined') {
      final roomJson = json['room'] as Map<String, dynamic>?;
      if (roomJson != null) {
        room.players
          ..clear()
          ..addAll(LobbyRoom.fromJson(roomJson).players);
      }
      _tryAutoStartGame();
      _status = 'Player joined: ${json['playerId'] ?? ''}';
      notifyListeners();
      return;
    }

    if (type == 'GameSnapshot' || type == 'CommandApplied') {
      final previousSnapshot = _snapshot;
      final stateJson =
          json['state'] as Map<String, dynamic>? ?? <String, dynamic>{};
      _snapshot = GameSnapshot.fromJson(stateJson);
      if (isGameInitialized) {
        _autoStartRequested = false;
      }
      _syncSelectedCardWithSnapshot();
      _status = type == 'CommandApplied'
          ? 'Applied: ${((json['command'] as Map<String, dynamic>? ?? <String, dynamic>{})['type'] ?? '').toString()}'
          : 'Snapshot loaded.';
      _updateDerivedRoundState(
        previousSnapshot,
        _snapshot,
        establishBaselineOnly: !_hasEstablishedSnapshotBaseline,
      );
      _hasEstablishedSnapshotBaseline = true;
      notifyListeners();
      return;
    }

    if ((json['error'] ?? '').toString().isNotEmpty) {
      _status = json['error'].toString();
      notifyListeners();
    }
  }

  void _tryAutoStartGame() {
    if (_autoStartRequested || !canStartGame) {
      return;
    }

    startGame();
  }

  void _updateDerivedRoundState(
    GameSnapshot? previousSnapshot,
    GameSnapshot? currentSnapshot, {
    bool establishBaselineOnly = false,
  }) {
    if (establishBaselineOnly) {
      return;
    }

    final completedRound = _buildCompletedRound(
      previousSnapshot,
      currentSnapshot,
    );
    if (completedRound != null) {
      _completedRounds.add(completedRound);
      roundOverController.setLastRound(completedRound);
    }

    final scoreHistory = _buildScoreHistory();
    if (scoreHistory != null) {
      scoreHistoryController.setViewModel(scoreHistory);
    }
  }

  RoundOverViewModel? _buildCompletedRound(
    GameSnapshot? previousSnapshot,
    GameSnapshot? currentSnapshot,
  ) {
    if (previousSnapshot == null || currentSnapshot == null) {
      return null;
    }

    if (previousSnapshot.roundNumber <= 0) {
      return null;
    }

    if (currentSnapshot.roundNumber <= previousSnapshot.roundNumber) {
      return null;
    }

    final previousRound = currentSnapshot.previousRound;
    final playerScores =
        previousRound?.playerScores ?? const <PreviousRoundPlayerScore>[];
    final currentPlayersById = <String, GamePlayer>{
      for (final GamePlayer player in currentSnapshot.players)
        player.id: player,
    };

    final players = playerScores
        .map(
          (PreviousRoundPlayerScore score) => RoundOverPlayerRowViewModel(
            playerId: score.playerName,
            tricksPoints: score.tricksPoints,
            opponentsRemainingCardsPoints: score.opponentsRemainingCardsPoints,
            haggisPoints: score.haggisPoints,
            roundPoints: score.roundPoints,
            totalPoints: currentPlayersById[score.playerName]?.score ?? 0,
          ),
        )
        .toList(growable: false);

    final sequence = previousSnapshot.appliedMoves.isNotEmpty
        ? previousSnapshot.appliedMoves
              .map((TrickMove move) => '${move.playerId}: ${move.description}')
              .toList(growable: false)
        : previousSnapshot.trick
              .map((TrickMove move) => '${move.playerId}: ${move.description}')
              .toList(growable: false);

    return RoundOverViewModel(
      gameId: room.gameId,
      roundNumber: previousSnapshot.roundNumber,
      nextRoundNumber: currentSnapshot.roundNumber,
      status:
          'Round ${previousSnapshot.roundNumber} finished. Round ${currentSnapshot.roundNumber} started.',
      winnerPlayerId: previousRound?.winnerPlayerName ?? '',
      players: players,
      haggisCards: List<String>.unmodifiable(
        previousRound?.haggisCards ?? const <String>[],
      ),
      lastSequenceLines: List<String>.unmodifiable(sequence),
    );
  }

  ScoreHistoryViewModel? _buildScoreHistory() {
    if (_completedRounds.isEmpty) {
      return null;
    }

    final roundNumbers =
        _completedRounds
            .map((RoundOverViewModel round) => round.roundNumber)
            .toSet()
            .toList()
          ..sort();

    final playerIds = <String>{
      for (final RoundOverViewModel round in _completedRounds)
        for (final RoundOverPlayerRowViewModel player in round.players)
          player.playerId,
      for (final GamePlayer player
          in _snapshot?.players ?? const <GamePlayer>[])
        player.id,
    }.toList()..sort();

    final currentTotals = <String, int>{
      for (final GamePlayer player
          in _snapshot?.players ?? const <GamePlayer>[])
        player.id: player.score,
    };

    final rows = playerIds
        .map((String playerId) {
          final perRound = roundNumbers
              .map(
                (int roundNumber) => _completedRounds
                    .where(
                      (RoundOverViewModel round) =>
                          round.roundNumber == roundNumber,
                    )
                    .expand((RoundOverViewModel round) => round.players)
                    .firstWhere(
                      (RoundOverPlayerRowViewModel row) =>
                          row.playerId == playerId,
                      orElse: () => const RoundOverPlayerRowViewModel(
                        playerId: '',
                        tricksPoints: 0,
                        opponentsRemainingCardsPoints: 0,
                        haggisPoints: 0,
                        roundPoints: 0,
                        totalPoints: 0,
                      ),
                    )
                    .roundPoints,
              )
              .toList(growable: false);

          return ScoreHistoryPlayerRowViewModel(
            playerId: playerId,
            roundPoints: perRound,
            totalPoints: currentTotals[playerId] ?? 0,
          );
        })
        .toList(growable: false);

    return ScoreHistoryViewModel(
      gameId: room.gameId,
      roundNumbers: List<int>.unmodifiable(roundNumbers),
      players: rows,
    );
  }

  Color _resolveActionColor(PossibleAction action) {
    final text = action.displayAction.toUpperCase();
    if (text.contains('BOMB')) {
      return const Color(0xFF8C2F39);
    }
    if (action.type.toLowerCase() == 'pass') {
      return const Color(0xFF5D6D69);
    }
    return const Color(0xFF0F766E);
  }

  void _syncSelectedCardWithSnapshot() {
    if (_selectedCards.isEmpty) {
      return;
    }

    GamePlayer? currentPlayer;
    for (final GamePlayer player
        in _snapshot?.players ?? const <GamePlayer>[]) {
      if (player.id == playerId) {
        currentPlayer = player;
        break;
      }
    }

    final hand = currentPlayer?.hand ?? const <String>[];
    _selectedCards.removeWhere((String card) => !hand.contains(card));
    if (isCurrentPlayersTurn &&
        _selectedCards.isNotEmpty &&
        !_canMatchAnyAction(_selectedCards)) {
      _selectedCards.clear();
    }
    _syncWildAssignmentsWithSelection();
  }

  bool _canMatchAnyAction(List<String> selectedCards) {
    if (!isCurrentPlayersTurn) {
      return false;
    }

    final selected = List<String>.from(selectedCards)..sort();
    for (final PossibleAction action
        in _snapshot?.possibleActions ?? const <PossibleAction>[]) {
      if (action.type.toLowerCase() == 'pass') {
        continue;
      }

      final labels = _extractSelectionCardsFromAction(action.displayAction)
        ..sort();
      if (_isSubset(selected, labels)) {
        return true;
      }
    }

    return false;
  }

  List<PossibleActionViewModel> _matchingPlayableActionsIgnoringWild(
    String ignoredWildCard,
  ) {
    final matches = _findMatchingPlayableActionsForSelection(
      _selectedCards,
      wildAssignments: _wildAssignments,
    );

    if (matches.isEmpty) {
      return const <PossibleActionViewModel>[];
    }

    return matches
        .where(
          (PossibleActionViewModel action) => _matchesWildAssignments(
            action,
            wildAssignments: _wildAssignments,
            ignoredWildCard: ignoredWildCard,
          ),
        )
        .toList(growable: false);
  }

  List<PossibleActionViewModel> _findMatchingPlayableActionsForSelection(
    List<String> cards, {
    required Map<String, String> wildAssignments,
  }) {
    if (cards.isEmpty || !isCurrentPlayersTurn) {
      return const <PossibleActionViewModel>[];
    }

    final selected = List<String>.from(cards)..sort();
    final matches = <PossibleActionViewModel>[];

    for (final PossibleAction action
        in _snapshot?.possibleActions ?? const <PossibleAction>[]) {
      if (action.type.toLowerCase() == 'pass') {
        continue;
      }

      final labels = _extractSelectionCardsFromAction(action.displayAction)
        ..sort();
      if (!_sameCards(labels, selected)) {
        continue;
      }

      final actionViewModel = PossibleActionViewModel(
        type: action.type,
        displayAction: action.displayAction,
        accentColor: _resolveActionColor(action),
      );

      if (_matchesWildAssignments(
        actionViewModel,
        wildAssignments: wildAssignments,
      )) {
        matches.add(actionViewModel);
      }
    }

    return matches;
  }

  bool _matchesWildAssignments(
    PossibleActionViewModel action, {
    required Map<String, String> wildAssignments,
    String? ignoredWildCard,
  }) {
    for (final MapEntry<String, String> entry in wildAssignments.entries) {
      if (entry.key == ignoredWildCard) {
        continue;
      }

      final assignment = _extractWildAssignment(
        action.displayAction,
        entry.key,
      );
      if (assignment == null ||
          assignment.toUpperCase() != entry.value.toUpperCase()) {
        return false;
      }
    }

    return true;
  }

  void _syncWildAssignmentsWithSelection() {
    _wildAssignments.removeWhere(
      (String key, String value) => !_selectedCards.contains(key),
    );

    final wildCards = _selectedCards.where(isWildCard).toList(growable: false);
    for (final String wildCard in wildCards) {
      final options = getWildReplacementOptions(wildCard);
      if (options.isEmpty) {
        _wildAssignments.remove(wildCard);
        continue;
      }

      final current = _wildAssignments[wildCard];
      if (current != null && options.contains(current)) {
        continue;
      }

      if (options.length == 1) {
        _wildAssignments[wildCard] = options.first;
      } else {
        _wildAssignments.remove(wildCard);
      }
    }
  }
}

List<String> _extractSelectionCardsFromAction(String action) {
  final selectedCards = <String>[];

  for (final String token in _extractActionParts(action)) {
    if (token.isEmpty) {
      continue;
    }

    final wildMatch = RegExp(r'^([JQK])(?:\[[^\]]+\])?$').firstMatch(token);
    if (wildMatch != null) {
      selectedCards.add(wildMatch.group(1)!);
      continue;
    }

    final normalMatch = RegExp(r'^(10|[2-9A])[BGROY]$').firstMatch(token);
    if (normalMatch != null) {
      selectedCards.add(token);
    }
  }

  return selectedCards;
}

String? _extractWildAssignment(String action, String wildCard) {
  final parts = _extractActionParts(action);
  for (final String token in parts) {
    final match = RegExp(
      '^${RegExp.escape(wildCard.toUpperCase())}\\[(.+)\\]\$',
    ).firstMatch(token);
    if (match != null) {
      return match.group(1)?.trim().toUpperCase();
    }
  }

  return null;
}

List<String> _extractActionParts(String action) {
  final start = action.indexOf('[');
  if (start < 0) {
    return const <String>[];
  }

  final parts = <String>[];
  final token = StringBuffer();
  var nestedBracketDepth = 0;

  for (var index = start + 1; index < action.length; index++) {
    final char = action[index];

    if (char == '[') {
      nestedBracketDepth++;
      token.write(char);
      continue;
    }

    if (char == ']') {
      if (nestedBracketDepth == 0) {
        final value = token.toString().trim().toUpperCase();
        if (value.isNotEmpty) {
          parts.add(value);
        }
        break;
      }

      nestedBracketDepth--;
      token.write(char);
      continue;
    }

    if (char == '|' && nestedBracketDepth == 0) {
      final value = token.toString().trim().toUpperCase();
      if (value.isNotEmpty) {
        parts.add(value);
      }
      token.clear();
      continue;
    }

    token.write(char);
  }

  return List<String>.unmodifiable(parts);
}

bool _sameCards(List<String> left, List<String> right) {
  if (left.length != right.length) {
    return false;
  }

  for (var index = 0; index < left.length; index++) {
    if (left[index] != right[index]) {
      return false;
    }
  }

  return true;
}

bool _isSubset(List<String> subset, List<String> superset) {
  if (subset.length > superset.length) {
    return false;
  }

  final remaining = List<String>.from(superset);
  for (final String item in subset) {
    final index = remaining.indexOf(item);
    if (index < 0) {
      return false;
    }
    remaining.removeAt(index);
  }

  return true;
}
