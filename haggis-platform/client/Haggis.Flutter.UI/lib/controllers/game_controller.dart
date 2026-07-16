import 'dart:async';
import 'dart:collection';

import 'package:flutter/material.dart';

import '../infrastructure/logging/app_logger.dart';
import '../infrastructure/remote/remote_game_websocket_client.dart';
import '../local_game/local_game_engine.dart';
import '../models/game_models.dart';
import '../models/lobby_models.dart';
import '../models/single_player_models.dart';
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
    this.singlePlayer = false,
    this.singlePlayerAiPlayers = const <SinglePlayerAiConfig>[],
  });

  final String serverBaseUrl;
  final String playerId;
  final LobbyRoom room;
  final bool singlePlayer;
  final List<SinglePlayerAiConfig> singlePlayerAiPlayers;

  RemoteGameWebSocketClient? _client;
  LocalGameEngine? _localEngine;
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
  bool _processingCommandQueue = false;
  bool _disposed = false;
  bool _commandInFlight = false;
  int _appliedMovesReplayGeneration = 0;
  int _collectAnimationGeneration = 0;
  List<TrickMove>? _visibleTrickReplay;
  TrickCollectViewModel? _collectingTrick;
  RoundOverViewModel? _deferredRoundOver;
  final Queue<Map<String, dynamic>> _pendingCommandMessages =
      Queue<Map<String, dynamic>>();

  GameSnapshot? get snapshot => _snapshot;

  String get status => _status;
  List<String> get selectedCards => List<String>.unmodifiable(_selectedCards);
  Map<String, String> get wildAssignments =>
      Map<String, String>.unmodifiable(_wildAssignments);

  bool get canStartGame =>
      (singlePlayer ||
          (room.players.length >= 2 && room.players.length <= 3)) &&
      room.players.isNotEmpty &&
      room.players.first == playerId &&
      !isGameInitialized;

  bool get isCurrentPlayersTurn => _snapshot?.currentPlayerId == playerId;

  bool get isGameInitialized =>
      (_snapshot?.version ?? 0) > 0 &&
      (_snapshot?.currentPlayerId ?? '').isNotEmpty;

  bool get canPass =>
      (_snapshot?.possibleActions ?? const <PossibleAction>[]).any(
        (PossibleAction action) => action.type.toLowerCase() == 'pass',
      ) &&
      !_commandInFlight;

  bool get canPlaySelectedCards =>
      matchingPlayableActions.isNotEmpty && !_commandInFlight;

  PossibleActionViewModel? get selectedPlayableAction {
    final matches = matchingPlayableActions;
    return matches.length == 1 ? matches.first : null;
  }

  List<PossibleActionViewModel> get matchingPlayableActions {
    return findMatchingPlayableActions(
      _selectedCards,
      wildAssignments: _wildAssignments,
    );
  }

  bool isTrickInPossibleActions(
    List<String> cards, {
    Map<String, String>? wildAssignments,
  }) {
    return findMatchingPlayableActions(
      cards,
      wildAssignments: wildAssignments ?? _wildAssignments,
    ).isNotEmpty;
  }

  List<PossibleActionViewModel> findMatchingPlayableActions(
    List<String> cards, {
    Map<String, String>? wildAssignments,
  }) {
    return _findMatchingPlayableActionsForSelection(
      cards,
      wildAssignments: wildAssignments ?? _wildAssignments,
    );
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
    final visibleTrick =
        _visibleTrickReplay ?? snapshot?.trick ?? const <TrickMove>[];
    final visibleCurrentTrick = _visibleTrickReplay == null
        ? snapshot?.trick ?? const <TrickMove>[]
        : _resolveVisibleCurrentTrick(
            visibleTrick,
            snapshot?.trick ?? const <TrickMove>[],
          );
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
      players:
          _orderedPlayers(
                snapshot?.players ?? const <GamePlayer>[],
                currentPlayerId,
              )
              .map(
                (GamePlayer player) => GamePlayerViewModel(
                  id: player.id,
                  score: player.score,
                  handCount: player.handCount,
                  finished: player.finished,
                  finishPosition: player.finishPosition,
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
      trick: visibleTrick
          .map(
            (TrickMove move) => TrickMoveViewModel(
              playerId: move.playerId,
              description: move.description,
            ),
          )
          .toList(growable: false),
      currentTrick: visibleCurrentTrick
          .map(
            (TrickMove move) => TrickMoveViewModel(
              playerId: move.playerId,
              description: move.description,
            ),
          )
          .toList(growable: false),
      collectingTrick: _collectingTrick,
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
      singlePlayer: singlePlayer,
    );
  }

  Future<void> connect() async {
    if (singlePlayer) {
      AppLogger.info('GameCtrl', 'Starting local game ${room.gameId}.');
      _localEngine = LocalGameEngine(
        humanId: playerId,
        aiPlayers: singlePlayerAiPlayers,
        seed: _forcedStartSeed,
      );
      _acceptLocalSnapshot(
        _localEngine!.start(),
        status: 'Local game started.',
      );
      return;
    }
    AppLogger.info(
      'GameCtrl',
      'Connecting to game room ${room.gameId} via $serverBaseUrl as $playerId.',
    );
    _client = RemoteGameWebSocketClient(
      serverBaseUrl: serverBaseUrl,
      gameId: room.gameId,
    );
    await _client!.connect();
    _subscription = _client!.messages.listen(
      _onMessage,
      onError: (Object error, StackTrace _) {
        _status = 'Game socket error: $error';
        AppLogger.error('GameCtrl', 'Game subscription error.', error);
        notifyListeners();
      },
      onDone: () {
        _status = 'Game socket closed.';
        AppLogger.warn('GameCtrl', 'Game subscription closed.');
        notifyListeners();
      },
    );
    joinGame();
    requestSnapshot();
  }

  void joinGame() {
    AppLogger.info('GameCtrl', 'Joining game ${room.gameId} as $playerId.');
    _client!.join(playerId);
  }

  void requestSnapshot() {
    AppLogger.info('GameCtrl', 'Requesting snapshot for ${room.gameId}.');
    _client!.requestSnapshot(playerId);
  }

  void startGame() {
    if (singlePlayer) {
      return;
    }
    if (!canStartGame) {
      return;
    }

    _autoStartRequested = true;
    AppLogger.info(
      'GameCtrl',
      'Sending create game for ${room.gameId}. seed=$_forcedStartSeed players=${singlePlayer ? _buildSinglePlayerRoster() : room.players.length}',
    );
    _client!.createGame(
      playerId,
      singlePlayer ? 3 : room.players.length,
      seed: _forcedStartSeed,
      players: singlePlayer ? _buildSinglePlayerRoster() : null,
    );
    _status = 'Start command sent. Seed: $_forcedStartSeed';
    notifyListeners();
  }

  List<Map<String, Object?>> _buildSinglePlayerRoster() {
    return <Map<String, Object?>>[
      <String, Object?>{'id': playerId},
      ...singlePlayerAiPlayers.map(
        (SinglePlayerAiConfig config) => <String, Object?>{
          'id': config.name,
          'type': 'ai',
          'ai': <String, Object?>{'difficulty': config.difficulty.value},
        },
      ),
    ];
  }

  void playAction(PossibleActionViewModel action) {
    if (_commandInFlight) {
      return;
    }

    _commandInFlight = true;
    AppLogger.info('GameCtrl', 'Sending action ${action.displayAction}.');
    if (singlePlayer) {
      _status = 'Playing: ${action.displayAction}';
      _selectedCards.clear();
      _wildAssignments.clear();
      notifyListeners();
      unawaited(_playLocal(action));
      return;
    }
    if (action.type.toLowerCase() == 'pass') {
      _client!.sendPass(playerId);
    } else {
      _client!.sendPlay(playerId, action.displayAction);
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
    return isCurrentPlayersTurn && !_commandInFlight && canSelectCard(card);
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
    return getWildReplacementOptionsForCards(wildCard, _selectedCards);
  }

  List<String> getWildReplacementOptionsForCards(
    String wildCard,
    List<String> cards, {
    Map<String, String>? wildAssignments,
  }) {
    if (!isWildCard(wildCard) || !cards.contains(wildCard)) {
      return const <String>[];
    }

    final effectiveWildAssignments = wildAssignments ?? _wildAssignments;
    final matches = _findCandidatePlayableActionsForSelection(cards);
    if (matches.isEmpty) {
      return const <String>[];
    }

    final options = <String>{};
    for (final PossibleActionViewModel action in matches.where(
      (PossibleActionViewModel action) => _matchesWildAssignments(
        action,
        wildAssignments: effectiveWildAssignments,
        ignoredWildCard: wildCard,
      ),
    )) {
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

  Future<void> _playLocal(PossibleActionViewModel action) async {
    try {
      final engine = _localEngine;
      if (engine == null || _disposed) return;
      final next = action.type.toLowerCase() == 'pass'
          ? await engine.pass()
          : await engine.play(action.displayAction);
      if (_disposed) return;
      _acceptLocalSnapshot(next, status: 'Local move applied.');
    } catch (error) {
      if (_disposed) return;
      _commandInFlight = false;
      _status = 'Local game error: $error';
      AppLogger.error('GameCtrl', 'Local move failed.', error);
      notifyListeners();
    }
  }

  void _acceptLocalSnapshot(GameSnapshot next, {required String status}) {
    final previous = _snapshot;
    _snapshot = next;
    final collectingTrick = _buildCollectingTrickForTransition(previous, next);
    final replayStarted = _startAppliedMovesReplay(
      previous,
      next,
      collectingTrick,
    );
    _commandInFlight = false;
    _autoStartRequested = false;
    _syncSelectedCardWithSnapshot();
    _status = status;
    _updateDerivedRoundState(
      previous,
      next,
      establishBaselineOnly: !_hasEstablishedSnapshotBaseline,
      deferRoundOverPopup: replayStarted || collectingTrick != null,
    );
    if (!replayStarted) {
      if (collectingTrick != null) {
        _startCollectAnimation(collectingTrick);
      } else {
        _publishDeferredRoundOver();
      }
    }
    _hasEstablishedSnapshotBaseline = true;
    notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    _pendingCommandMessages.clear();
    _appliedMovesReplayGeneration++;
    _collectAnimationGeneration++;
    _visibleTrickReplay = null;
    _collectingTrick = null;
    _deferredRoundOver = null;
    _subscription?.cancel();
    _client?.dispose();
    roundOverController.dispose();
    scoreHistoryController.dispose();
    super.dispose();
  }

  @visibleForTesting
  void applySnapshotForTesting(GameSnapshot snapshot) {
    _snapshot = snapshot;
    _status = 'Testing snapshot applied.';
    _syncSelectedCardWithSnapshot();
  }

  void applyStateMessageForTesting(Map<String, dynamic> json) {
    _applyStateMessage(json);
  }

  void _onMessage(Map<String, dynamic> json) {
    final type = (json['type'] ?? '').toString();
    AppLogger.info('GameCtrl', 'Received message type=$type payload=$json');

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

    if (type == 'CommandApplied') {
      _commandInFlight = false;
      _pendingCommandMessages.add(json);
      unawaited(_processCommandQueue());
      return;
    }

    if (type == 'GameSnapshot') {
      _commandInFlight = false;
      _pendingCommandMessages.clear();
      _cancelAppliedMovesReplay();
      _applyStateMessage(json);
      return;
    }

    if ((json['error'] ?? '').toString().isNotEmpty) {
      _commandInFlight = false;
      _status = json['error'].toString();
      notifyListeners();
    }
  }

  Future<void> _processCommandQueue() async {
    if (_processingCommandQueue) {
      return;
    }

    _processingCommandQueue = true;
    try {
      while (_pendingCommandMessages.isNotEmpty) {
        if (_disposed) {
          return;
        }
        final json = _pendingCommandMessages.removeFirst();
        _applyStateMessage(json);
        if (_pendingCommandMessages.isNotEmpty && _isAiCommandMessage(json)) {
          await Future<void>.delayed(const Duration(milliseconds: 700));
        }
      }
    } finally {
      _processingCommandQueue = false;
    }
  }

  void _applyStateMessage(Map<String, dynamic> json) {
    if (_disposed) {
      return;
    }

    final type = (json['type'] ?? '').toString();
    final previousSnapshot = _snapshot;
    final stateJson =
        json['state'] as Map<String, dynamic>? ?? <String, dynamic>{};
    _snapshot = GameSnapshot.fromJson(stateJson);
    final collectingTrick = type == 'CommandApplied'
        ? _buildCollectingTrickForTransition(previousSnapshot, _snapshot)
        : null;
    final replayStarted = type == 'CommandApplied'
        ? _startAppliedMovesReplay(previousSnapshot, _snapshot, collectingTrick)
        : false;
    if (!replayStarted && type != 'CommandApplied') {
      _visibleTrickReplay = null;
    }
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
      deferRoundOverPopup: replayStarted || collectingTrick != null,
    );
    if (!replayStarted) {
      if (collectingTrick != null) {
        _startCollectAnimation(collectingTrick);
      } else if (type != 'CommandApplied') {
        _cancelCollectAnimation();
        _publishDeferredRoundOver();
      } else {
        _publishDeferredRoundOver();
      }
    }
    _hasEstablishedSnapshotBaseline = true;
    notifyListeners();
    return;
  }

  bool _isAiCommandMessage(Map<String, dynamic> json) {
    final commandJson = json['command'] as Map<String, dynamic>?;
    final commandPlayerId = (commandJson?['playerId'] ?? '').toString();
    if (commandPlayerId.isEmpty || commandPlayerId == playerId) {
      return false;
    }

    return singlePlayerAiPlayers.any(
      (SinglePlayerAiConfig config) => config.name == commandPlayerId,
    );
  }

  bool _startAppliedMovesReplay(
    GameSnapshot? previousSnapshot,
    GameSnapshot? currentSnapshot,
    TrickCollectViewModel? collectingTrick,
  ) {
    final appliedMoves = currentSnapshot?.appliedMoves ?? const <TrickMove>[];
    if (appliedMoves.length <= 1) {
      _cancelAppliedMovesReplay();
      return false;
    }

    final baseTrick = previousSnapshot?.trick ?? const <TrickMove>[];
    final generation = ++_appliedMovesReplayGeneration;
    _visibleTrickReplay = List<TrickMove>.unmodifiable(<TrickMove>[
      ...baseTrick,
      appliedMoves.first,
    ]);
    unawaited(
      _replayAppliedMoves(generation, baseTrick, appliedMoves, collectingTrick),
    );
    return true;
  }

  Future<void> _replayAppliedMoves(
    int generation,
    List<TrickMove> baseTrick,
    List<TrickMove> appliedMoves,
    TrickCollectViewModel? collectingTrick,
  ) async {
    for (var index = 1; index < appliedMoves.length; index++) {
      await Future<void>.delayed(const Duration(milliseconds: 700));
      if (_disposed || generation != _appliedMovesReplayGeneration) {
        return;
      }

      _visibleTrickReplay = List<TrickMove>.unmodifiable(<TrickMove>[
        ...baseTrick,
        ...appliedMoves.take(index + 1),
      ]);
      notifyListeners();
    }

    if (collectingTrick != null) {
      await Future<void>.delayed(const Duration(milliseconds: 450));
      if (_disposed || generation != _appliedMovesReplayGeneration) {
        return;
      }

      _visibleTrickReplay = null;
      _startCollectAnimation(collectingTrick);
      return;
    }

    _publishDeferredRoundOver();
  }

  void _cancelAppliedMovesReplay() {
    _appliedMovesReplayGeneration++;
    _visibleTrickReplay = null;
  }

  TrickCollectViewModel? _buildCollectingTrickForTransition(
    GameSnapshot? previousSnapshot,
    GameSnapshot? currentSnapshot,
  ) {
    if (currentSnapshot == null || currentSnapshot.trick.isNotEmpty) {
      return null;
    }

    final completedTrick =
        <TrickMove>[
              ...(previousSnapshot?.trick ?? const <TrickMove>[]),
              ...currentSnapshot.appliedMoves,
            ]
            .where(
              (TrickMove move) => _moveCardLabels(move.description).isNotEmpty,
            )
            .toList(growable: false);
    if (completedTrick.isEmpty) {
      return null;
    }

    final winnerPlayerId = _resolveCollectWinnerPlayerId(
      completedTrick,
      fallbackPlayerId: currentSnapshot.currentPlayerId,
    );
    final winnerIndex = currentSnapshot.players.indexWhere(
      (GamePlayer player) => player.id == winnerPlayerId,
    );
    final cards = completedTrick
        .expand((TrickMove move) => _moveCardLabels(move.description))
        .toList(growable: false);

    return TrickCollectViewModel(
      winnerPlayerId: winnerPlayerId,
      winnerIndex: winnerIndex < 0 ? 0 : winnerIndex,
      playerCount: currentSnapshot.players.length,
      cards: List<String>.unmodifiable(cards),
    );
  }

  String _resolveCollectWinnerPlayerId(
    List<TrickMove> completedTrick, {
    required String fallbackPlayerId,
  }) {
    TrickMove? bestNonBombMove;
    for (final TrickMove move in completedTrick) {
      final description = move.description.trim().toUpperCase();
      if (description.isEmpty ||
          description == 'PASS' ||
          description == 'PASS[]' ||
          description.startsWith('BOMB[')) {
        continue;
      }

      bestNonBombMove = move;
    }

    return bestNonBombMove?.playerId ?? fallbackPlayerId;
  }

  void _startCollectAnimation(TrickCollectViewModel collectingTrick) {
    final generation = ++_collectAnimationGeneration;
    _collectingTrick = collectingTrick;
    notifyListeners();
    unawaited(_clearCollectAnimation(generation));
  }

  Future<void> _clearCollectAnimation(int generation) async {
    await Future<void>.delayed(const Duration(milliseconds: 900));
    if (_disposed || generation != _collectAnimationGeneration) {
      return;
    }

    _collectingTrick = null;
    _publishDeferredRoundOver();
    notifyListeners();
  }

  void _cancelCollectAnimation() {
    _collectAnimationGeneration++;
    _collectingTrick = null;
  }

  void _publishDeferredRoundOver() {
    final roundOver = _deferredRoundOver;
    if (roundOver == null) {
      return;
    }

    _deferredRoundOver = null;
    roundOverController.setLastRound(roundOver);
    notifyListeners();
  }

  void _tryAutoStartGame() {
    if (_autoStartRequested || !canStartGame) {
      return;
    }

    startGame();
  }

  List<GamePlayer> _orderedPlayers(
    List<GamePlayer> players,
    String currentPlayerId,
  ) {
    if (players.isEmpty || currentPlayerId.isEmpty) {
      return players;
    }

    final currentIndex = players.indexWhere(
      (GamePlayer player) => player.id == currentPlayerId,
    );
    if (currentIndex <= 0) {
      return players;
    }

    return List<GamePlayer>.unmodifiable(<GamePlayer>[
      ...players.skip(currentIndex),
      ...players.take(currentIndex),
    ]);
  }

  List<TrickMove> _resolveVisibleCurrentTrick(
    List<TrickMove> visibleTrick,
    List<TrickMove> snapshotTrick,
  ) {
    if (visibleTrick.isEmpty || snapshotTrick.isEmpty) {
      return const <TrickMove>[];
    }

    for (var length = snapshotTrick.length; length >= 1; length--) {
      if (visibleTrick.length < length) {
        continue;
      }

      final visibleSuffix = visibleTrick.sublist(visibleTrick.length - length);
      final snapshotPrefix = snapshotTrick.take(length).toList(growable: false);
      if (_sameTrickMoves(visibleSuffix, snapshotPrefix)) {
        return List<TrickMove>.unmodifiable(visibleSuffix);
      }
    }

    return const <TrickMove>[];
  }

  void _updateDerivedRoundState(
    GameSnapshot? previousSnapshot,
    GameSnapshot? currentSnapshot, {
    bool establishBaselineOnly = false,
    bool deferRoundOverPopup = false,
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
      if (deferRoundOverPopup) {
        _deferredRoundOver = completedRound;
      } else {
        roundOverController.setLastRound(completedRound);
      }
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

    final previousRound = currentSnapshot.previousRound;
    if (previousRound == null) {
      return null;
    }

    final roundAdvanced =
        currentSnapshot.roundNumber > previousSnapshot.roundNumber;
    final gameFinishedOnCurrentRound =
        currentSnapshot.gameOver &&
        previousRound.roundNumber == previousSnapshot.roundNumber;
    if (!roundAdvanced && !gameFinishedOnCurrentRound) {
      return null;
    }

    if (_completedRounds.any(
      (RoundOverViewModel round) =>
          round.roundNumber == previousRound.roundNumber,
    )) {
      return null;
    }

    final playerScores = previousRound.playerScores;
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
      roundNumber: previousRound.roundNumber,
      nextRoundNumber: currentSnapshot.roundNumber,
      status: currentSnapshot.gameOver
          ? 'Round ${previousRound.roundNumber} finished. Game over.'
          : 'Round ${previousRound.roundNumber} finished. Round ${currentSnapshot.roundNumber} started.',
      winnerPlayerId: previousRound.winnerPlayerName,
      players: players,
      haggisCards: List<String>.unmodifiable(previousRound.haggisCards),
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

  List<PossibleActionViewModel> _findMatchingPlayableActionsForSelection(
    List<String> cards, {
    required Map<String, String> wildAssignments,
  }) {
    if (cards.isEmpty || !isCurrentPlayersTurn) {
      return const <PossibleActionViewModel>[];
    }

    final matches = <PossibleActionViewModel>[];
    for (final actionViewModel in _findCandidatePlayableActionsForSelection(
      cards,
    )) {
      if (_matchesWildAssignments(
        actionViewModel,
        wildAssignments: wildAssignments,
      )) {
        matches.add(actionViewModel);
      }
    }

    return matches;
  }

  List<PossibleActionViewModel> _findCandidatePlayableActionsForSelection(
    List<String> cards,
  ) {
    if (cards.isEmpty || !isCurrentPlayersTurn) {
      return const <PossibleActionViewModel>[];
    }

    final selected = List<String>.from(cards)..sort();
    final matches = <PossibleActionViewModel>[];
    final seenDisplayActions = <String>{};

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

      if (seenDisplayActions.add(actionViewModel.displayAction)) {
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

List<String> _moveCardLabels(String action) {
  final labels = <String>[];
  for (final String token in _extractActionParts(action)) {
    final wildMatch = RegExp(
      r'^([JQK])(?:\[([^\]]+)\])?$',
      caseSensitive: false,
    ).firstMatch(token);
    if (wildMatch != null) {
      labels.add(
        (wildMatch.group(2)?.trim().isNotEmpty ?? false)
            ? wildMatch.group(2)!.trim().toUpperCase()
            : wildMatch.group(1)!.trim().toUpperCase(),
      );
      continue;
    }

    final normalMatch = RegExp(
      r'^(10|[2-9A])[BGROY]$',
      caseSensitive: false,
    ).firstMatch(token);
    if (normalMatch != null) {
      labels.add(token.toUpperCase());
    }
  }

  return List<String>.unmodifiable(labels);
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

bool _sameTrickMoves(List<TrickMove> left, List<TrickMove> right) {
  if (left.length != right.length) {
    return false;
  }

  for (var index = 0; index < left.length; index++) {
    if (left[index].playerId != right[index].playerId ||
        left[index].description != right[index].description) {
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
