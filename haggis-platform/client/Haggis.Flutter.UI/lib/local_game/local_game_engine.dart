import 'dart:async';
import 'dart:isolate';

import '../models/game_models.dart';
import '../models/single_player_models.dart';

/// Client-side Haggis session. This file deliberately has no Flutter or
/// networking imports, so AI work can be moved to an isolate.
class LocalGameEngine {
  LocalGameEngine({
    required this.humanId,
    required List<SinglePlayerAiConfig> aiPlayers,
    this.seed = 115826734,
  }) : _ai = {for (final config in aiPlayers) config.name: config.difficulty},
       _seating = <String>[humanId, ...aiPlayers.map((config) => config.name)];

  final String humanId;
  final int seed;
  final Map<String, AiDifficulty> _ai;
  final List<String> _seating;
  final Map<String, int> _totals = <String, int>{};
  final List<_Player> _players = <_Player>[];
  final List<_Move> _trick = <_Move>[];
  final List<_Move> _archive = <_Move>[];
  final List<String> _finishingOrder = <String>[];
  List<_Card> _haggisCards = <_Card>[];
  PreviousRound? _previousRound;
  int _round = 0;
  int _version = 0;
  int _current = 0;
  bool _gameOver = false;

  GameSnapshot start() {
    if (_seating.length < 2 || _seating.length > 3) {
      throw StateError('Haggis supports two or three players.');
    }
    for (final id in _seating) {
      _totals[id] = 0;
    }
    _newRound();
    return _snapshot();
  }

  Future<GameSnapshot> play(String description) async {
    if (_gameOver || _currentPlayer.id != humanId) return _snapshot();
    final action = _legalActions.firstWhere(
      (candidate) => candidate.description == description,
      orElse: () => throw StateError('Illegal action: $description'),
    );
    final applied = <_Move>[];
    _apply(action, applied);
    await _advanceAi(applied);
    _version++;
    return _snapshot(appliedMoves: applied);
  }

  Future<GameSnapshot> pass() async => play('Pass');

  _Player get _currentPlayer => _players[_current];

  List<_Action> get _legalActions {
    if (_gameOver || _roundOver) return const <_Action>[];
    final last = _lastNonPass?.action.trick;
    final actions =
        _generateTricks(_currentPlayer.hand)
            .where((trick) => last == null || trick.compareTo(last) > 0)
            .map((trick) => _Action(_currentPlayer.id, trick))
            .toList()
          ..sort((a, b) => a.trick!.compareTo(b.trick!));
    if (last != null) actions.add(_Action.pass(_currentPlayer.id));
    return actions;
  }

  Future<void> _advanceAi(List<_Move> applied) async {
    var safety = 0;
    while (!_gameOver && _currentPlayer.id != humanId && safety++ < 5000) {
      final actions = _legalActions;
      if (actions.isEmpty) break;
      final difficulty = _ai[_currentPlayer.id] ?? AiDifficulty.easy;
      final input = <String, Object?>{
        'difficulty': difficulty.value,
        'seed': seed + _round * 7919 + _archive.length + _trick.length,
        'actions': actions.map((action) => action.toMap()).toList(),
        'handCount': _currentPlayer.hand.length,
        'opening': _trick.isEmpty,
      };
      final selected = difficulty.value >= AiDifficulty.medium.value
          ? await Isolate.run(() => _chooseAiAction(input))
          : _chooseAiAction(input);
      final action = actions.firstWhere(
        (candidate) => candidate.description == selected,
        orElse: () => actions.first,
      );
      _apply(action, applied);
    }
  }

  static String _chooseAiAction(Map<String, Object?> input) {
    final actions = (input['actions']! as List<Object?>)
        .cast<Map<Object?, Object?>>();
    final difficulty = input['difficulty']! as int;
    final random = DotNetRandom(input['seed']! as int);
    if (difficulty == 1) {
      return actions[random.next(actions.length)]['description']! as String;
    }
    if (difficulty == 2) {
      return _chooseHeuristicAction(actions, input);
    }
    final playable = actions.where((a) => a['pass'] != true).toList();
    if (playable.isEmpty) return 'Pass';
    int score(Map<Object?, Object?> action) {
      final cards = action['cards']! as int;
      final rank = action['rank']! as int;
      final bomb = action['bomb'] == true;
      var value = cards * 100 - rank * 3 - (bomb ? 500 : 0);
      if (cards == input['handCount']) value += 100000;
      if (difficulty >= 3) {
        final simulations = difficulty == 3 ? 300 : 2000;
        var wins = 0;
        for (var i = 0; i < simulations; i++) {
          final noise = random.next(1000);
          if (value + noise > 500) wins++;
        }
        value += wins;
        if (difficulty == 5) value += cards * 15;
      }
      return value;
    }

    playable.sort((a, b) => score(b).compareTo(score(a)));
    return playable.first['description']! as String;
  }

  static String _chooseHeuristicAction(
    List<Map<Object?, Object?>> actions,
    Map<String, Object?> input,
  ) {
    final playable = actions.where((action) => action['pass'] != true).toList();
    if (playable.isEmpty) return 'Pass';
    final handCount = input['handCount']! as int;
    final opening = input['opening']! as bool;
    int weighted(double value) =>
        value >= 0 ? (value + 0.5).floor() : (value - 0.5).ceil();
    int continuationCount(Map<Object?, Object?> action) => playable
        .where(
          (other) =>
              other['type'] == action['type'] &&
              (other['rank']! as int) > (action['rank']! as int),
        )
        .length;
    final maxContinuations = playable
        .map(continuationCount)
        .fold(0, (maximum, count) => count > maximum ? count : maximum);
    int score(Map<Object?, Object?> action) {
      final cards = action['cards']! as int;
      final rank = action['rank']! as int;
      final wilds = action['wilds']! as int;
      final bomb = action['bomb'] == true;
      var result = 0;
      if (opening) {
        final phaseBias = ((handCount - 8) / 8).clamp(-1.0, 1.0);
        final rankBias = 1 - 2 * (rank - 2) / 11;
        result += weighted(weighted(10 * phaseBias * rankBias) * 1.983);
        final phase = ((handCount - 8) / 8).clamp(0.0, 1.0);
        final shorter = handCount <= 1
            ? 0.0
            : 1 - (cards - 1) / (handCount - 1);
        result += weighted(weighted(phase * shorter * 24) * 1.686);
        if (bomb) result += weighted(-weighted(100 * handCount / 17) * 0.608);
        if (wilds > 0) {
          result += weighted(
            weighted(-50 * wilds / cards * handCount / 17) * 4.418,
          );
        }
        if (action['type'] == 'SINGLE' && wilds == 0) {
          final identity = action['identities'] as List<Object?>;
          final breaks = playable.any(
            (other) =>
                other['type'] != 'SINGLE' &&
                (other['wilds']! as int) == 0 &&
                (other['identities']! as List<Object?>).contains(
                  identity.first,
                ),
          );
          if (!breaks) result += 40;
        }
        if (action['type'] != 'SINGLE' && maxContinuations > 0) {
          result += weighted(
            weighted(20 * continuationCount(action) / maxContinuations) * 2.455,
          );
        }
        final natural = (action['identities']! as List<Object?>).toSet();
        final related =
            natural.isNotEmpty &&
            playable.any(
              (other) =>
                  other != action &&
                  other['class'] == action['class'] &&
                  (other['cards']! as int) > cards &&
                  natural.every(
                    (card) =>
                        (other['identities']! as List<Object?>).contains(card),
                  ),
            );
        if (related) {
          result += weighted(weighted(-50 * handCount / 17) * 3.458);
        }
      } else {
        if (bomb) result += weighted(-40 * 3.646);
        final sameType = playable
            .where((other) => other['type'] == action['type'])
            .toList();
        final ranks =
            sameType.map((other) => other['rank']! as int).toSet().toList()
              ..sort();
        if (!bomb && ranks.length > 1) {
          final opportunity = ranks.length - 1 - ranks.indexOf(rank);
          result += weighted(
            weighted(20 * opportunity / (ranks.length - 1)) * 3.652,
          );
        }
        if (maxContinuations > 0) {
          result += weighted(50 * continuationCount(action) / maxContinuations);
        }
        final natural = (action['identities']! as List<Object?>).toSet();
        final related = playable.any(
          (other) =>
              other != action &&
              other['class'] == action['class'] &&
              (other['cards']! as int) > cards &&
              natural.every(
                (card) =>
                    (other['identities']! as List<Object?>).contains(card),
              ),
        );
        if (related) result += weighted(weighted(-40 * handCount / 17) * 0.224);
      }
      return result;
    }

    final indexed = playable.indexed.toList();
    indexed.sort((left, right) {
      final comparison = score(right.$2).compareTo(score(left.$2));
      return comparison != 0 ? comparison : left.$1.compareTo(right.$1);
    });
    final best = indexed.first.$2;
    if (!opening &&
        actions.any((action) => action['pass'] == true) &&
        score(best) <= 0) {
      return 'Pass';
    }
    return best['description']! as String;
  }

  void _apply(_Action action, List<_Move> applied) {
    final player = _currentPlayer;
    if (!action.isPass) {
      for (final card in action.trick!.cards) {
        player.hand.removeWhere((held) => held.identity == card.identity);
      }
    }
    final finalMove = !action.isPass && player.hand.isEmpty;
    final move = _Move(player.id, action, finalMove: finalMove);
    _trick.add(move);
    applied.add(move);
    if (finalMove && !_finishingOrder.contains(player.id)) {
      _finishingOrder.add(player.id);
      player.opponentsRemaining = _players
          .where((other) => other.id != player.id && other.hand.isNotEmpty)
          .fold(0, (sum, other) => sum + other.hand.length);
    }
    if (_roundOver) {
      _finishRound();
      return;
    }
    if (finalMove) {
      _advanceCurrent(removeCurrent: true);
    } else {
      _advanceCurrent();
    }
    if (_isEndingPass) _collectTrick();
  }

  bool get _roundOver =>
      _players.where((player) => player.hand.isNotEmpty).length == 1;

  void _advanceCurrent({bool removeCurrent = false}) {
    for (var offset = 1; offset <= _players.length; offset++) {
      final candidate = (_current + offset) % _players.length;
      if (_players[candidate].hand.isNotEmpty) {
        _current = candidate;
        return;
      }
    }
  }

  bool get _isEndingPass {
    if (_trick.isEmpty || !_trick.last.action.isPass) return false;
    final active = _players.where((player) => player.hand.isNotEmpty).length;
    if (active == 2) return true;
    if (_trick.length < 2) return false;
    if (_trick.any((move) => move.finalMove) &&
        _trick[_trick.length - 2].finalMove) {
      return false;
    }
    return _trick[_trick.length - 2].action.isPass;
  }

  _Move? get _lastNonPass {
    for (var i = _trick.length - 1; i >= 0; i--) {
      if (!_trick[i].action.isPass) return _trick[i];
    }
    return null;
  }

  void _collectTrick() {
    _Move? winner;
    for (final move in _trick) {
      if (!move.action.isPass && !move.action.trick!.isBomb) winner = move;
    }
    winner ??= _lastNonPass;
    if (winner != null) {
      final target = _players.firstWhere(
        (player) => player.id == winner!.playerId,
      );
      target.discard.addAll(
        _trick
            .where((move) => !move.action.isPass)
            .expand((move) => move.action.trick!.cards),
      );
      _current = _players.indexOf(target);
      if (target.hand.isEmpty) _advanceCurrent();
    }
    _archive.addAll(_trick);
    _trick.clear();
  }

  void _finishRound() {
    _archive.addAll(_trick);
    if (_finishingOrder.isEmpty) {
      _finishingOrder.addAll(
        _players.where((player) => player.hand.isEmpty).map((p) => p.id),
      );
    }
    final haggisPoints = _haggisCards.fold(
      0,
      (sum, card) => sum + _cardPoints(card),
    );
    final scores = <PreviousRoundPlayerScore>[];
    for (final player in _players) {
      final trickPoints = player.discard.fold(
        0,
        (sum, card) => sum + _cardPoints(card),
      );
      final remaining = player.opponentsRemaining < 0
          ? 0
          : player.opponentsRemaining * 5;
      final bonus = _finishingOrder.firstOrNull == player.id ? haggisPoints : 0;
      final roundPoints = trickPoints + remaining + bonus;
      _totals[player.id] = (_totals[player.id] ?? 0) + roundPoints;
      scores.add(
        PreviousRoundPlayerScore(
          playerName: player.id,
          tricksPoints: trickPoints,
          opponentsRemainingCardsPoints: remaining,
          haggisPoints: bonus,
          roundPoints: roundPoints,
        ),
      );
    }
    scores.sort((a, b) => b.roundPoints.compareTo(a.roundPoints));
    _previousRound = PreviousRound(
      roundNumber: _round,
      winnerPlayerName: scores.first.playerName,
      finishingOrderPlayerNames: <String>[
        ..._finishingOrder,
        ..._players
            .map((p) => p.id)
            .where((id) => !_finishingOrder.contains(id)),
      ],
      haggisCards: _haggisCards.map((card) => card.label).toList(),
      playerScores: scores,
    );
    _gameOver = _totals.values.any((score) => score >= 250);
    if (!_gameOver) _newRound();
  }

  void _newRound() {
    _round++;
    _trick.clear();
    _archive.clear();
    _finishingOrder.clear();
    final dealer = _deal(seed + _round * 7919, _seating.length);
    _haggisCards = dealer.haggis;
    _players
      ..clear()
      ..addAll(_seating.map((id) => _Player(id)));
    for (var i = 0; i < _players.length; i++) {
      _players[i].hand.addAll(dealer.hands[i]);
      _players[i].hand.sort(_Card.compare);
    }
    var starter = 0;
    for (var i = 1; i < _seating.length; i++) {
      if ((_totals[_seating[i]] ?? 0) < (_totals[_seating[starter]] ?? 0)) {
        starter = i;
      }
    }
    if (starter != 0) {
      final ordered = <_Player>[
        ..._players.skip(starter),
        ..._players.take(starter),
      ];
      _players
        ..clear()
        ..addAll(ordered);
    }
    _current = 0;
  }

  GameSnapshot _snapshot({List<_Move> appliedMoves = const <_Move>[]}) {
    return GameSnapshot(
      version: _version + 1,
      roundNumber: _round,
      currentPlayerId: _currentPlayer.id,
      roundOver: _roundOver,
      gameOver: _gameOver,
      players: _players
          .map(
            (player) => GamePlayer(
              id: player.id,
              score: _totals[player.id] ?? 0,
              handCount: player.hand.length,
              hand: player.hand.map((card) => card.label).toList(),
              finished: player.hand.isEmpty,
              finishPosition: _finishingOrder.indexOf(player.id) + 1,
            ),
          )
          .toList(),
      trick: _trick
          .map(
            (move) => TrickMove(
              playerId: move.playerId,
              description: move.description,
            ),
          )
          .toList(),
      possibleActions: _currentPlayer.id == humanId
          ? _legalActions
                .map(
                  (action) => PossibleAction(
                    type: action.isPass ? 'Pass' : 'Play',
                    displayAction: action.description,
                  ),
                )
                .toList()
          : const <PossibleAction>[],
      appliedMoves: appliedMoves
          .map(
            (move) => TrickMove(
              playerId: move.playerId,
              description: move.description,
            ),
          )
          .toList(),
      previousRound: _previousRound,
    );
  }

  static _Deal _deal(int seed, int playerCount) {
    final suits = playerCount == 3 ? 'RBGYO'.split('') : 'RBGO'.split('');
    final deck = <_Card>[
      for (final suit in suits)
        for (var rank = 2; rank <= 10; rank++) _Card(rank, suit),
    ];
    final random = DotNetRandom(seed);
    for (var n = deck.length - 1; n > 0; n--) {
      final k = random.next(n + 1);
      final value = deck[n];
      deck[n] = deck[k];
      deck[k] = value;
    }
    final hands = <List<_Card>>[];
    for (var i = 0; i < playerCount; i++) {
      hands.add(<_Card>[
        ...deck.take(14),
        _Card.wild(11),
        _Card.wild(12),
        _Card.wild(13),
      ]);
      deck.removeRange(0, 14);
    }
    return _Deal(hands, deck);
  }

  static int _cardPoints(_Card card) =>
      const <int, int>{
        3: 1,
        5: 1,
        7: 1,
        9: 1,
        11: 2,
        12: 3,
        13: 5,
      }[card.rank] ??
      0;
}

List<_Trick> _generateTricks(List<_Card> hand) {
  final result = <_Trick>[];
  final seen = <String>{};
  void add(_Trick trick) {
    if (seen.add(trick.description)) result.add(trick);
  }

  final wilds = hand.where((card) => card.isWild).toList();
  final natural = hand.where((card) => !card.isWild).toList();
  for (var count = 1; count <= 6; count++) {
    for (var rank = 2; rank <= 10; rank++) {
      final same = natural.where((card) => card.rank == rank).toList();
      for (var naturalCount = count; naturalCount >= 1; naturalCount--) {
        final wildCount = count - naturalCount;
        if (same.length < naturalCount || wilds.length < wildCount) continue;
        for (final bases in _combinations(same, naturalCount)) {
          for (final chosenWilds in _combinations(wilds, wildCount)) {
            add(
              _Trick(_sameName(count), <_Card>[
                ...bases,
                ...chosenWilds.map((w) => w.asRank(rank, bases.first.suit)),
              ]),
            );
          }
        }
      }
    }
    if (count == 1) {
      for (final wild in wilds) {
        add(_Trick('SINGLE', <_Card>[wild]));
      }
    }
  }
  for (var length = 3; length <= 7; length++) {
    for (final suit in 'RBGYO'.split('')) {
      for (var start = 2; start <= 13 - length + 1; start++) {
        final cards = <_Card>[];
        final missing = <int>[];
        for (var rank = start; rank < start + length; rank++) {
          final match = natural
              .where((card) => card.suit == suit && card.rank == rank)
              .firstOrNull;
          match == null ? missing.add(rank) : cards.add(match);
        }
        if (cards.isEmpty || missing.length > wilds.length) continue;
        for (final chosen in _combinations(wilds, missing.length)) {
          add(
            _Trick('SEQ$length', <_Card>[
              ...cards,
              for (var i = 0; i < chosen.length; i++)
                chosen[i].asRank(missing[i], suit),
            ]),
          );
        }
      }
    }
  }
  for (final groupSize in <int>[2, 3, 4, 5]) {
    final maxLength = <int, int>{2: 7, 3: 5, 4: 4, 5: 3}[groupSize]!;
    for (var length = 2; length <= maxLength; length++) {
      for (var start = 2; start <= 10 - length + 1; start++) {
        final selected = <_Card>[];
        var requiredWilds = 0;
        var valid = true;
        for (var rank = start; rank < start + length; rank++) {
          final same = natural
              .where((card) => card.rank == rank)
              .take(groupSize)
              .toList();
          if (same.isEmpty) {
            valid = false;
            break;
          }
          selected.addAll(same);
          requiredWilds += groupSize - same.length;
        }
        if (!valid || requiredWilds > wilds.length) continue;
        final replacements = <_Card>[];
        var wi = 0;
        for (var rank = start; rank < start + length; rank++) {
          final have = selected.where((card) => card.rank == rank).length;
          for (var j = have; j < groupSize; j++) {
            replacements.add(wilds[wi++].asRank(rank, 'R'));
          }
        }
        add(
          _Trick(_stairName(groupSize, length), <_Card>[
            ...selected,
            ...replacements,
          ]),
        );
      }
    }
  }
  final special = natural
      .where((card) => const <int>{3, 5, 7, 9}.contains(card.rank))
      .toList();
  for (final cards in _combinations(special, 4)) {
    if (cards.map((card) => card.rank).toSet().length == 4 &&
        (cards.map((c) => c.suit).toSet().length == 1 ||
            cards.map((c) => c.suit).toSet().length == 4)) {
      add(_Trick('BOMB', cards, bomb: true));
    }
  }
  for (var count = 2; count <= 3; count++) {
    for (final cards in _combinations(wilds, count)) {
      add(_Trick('BOMB', cards, bomb: true));
    }
  }
  return result;
}

String _sameName(int n) =>
    const <String>['', 'SINGLE', 'PAIR', 'TRIPLE', 'QUAD', 'FIVED', 'SIXED'][n];
String _stairName(int size, int length) => size == 2
    ? 'PAIRSEQ$length'
    : size == 3
    ? 'TRIPLESTAIR$length'
    : size == 4
    ? 'QUADSTAIR$length'
    : 'FIVEDSTAIR$length';

List<List<T>> _combinations<T>(List<T> values, int count) {
  if (count == 0) return <List<T>>[<T>[]];
  final result = <List<T>>[];
  void visit(int start, List<T> picked) {
    if (picked.length == count) {
      result.add(List<T>.of(picked));
      return;
    }
    for (var i = start; i <= values.length - (count - picked.length); i++) {
      picked.add(values[i]);
      visit(i + 1, picked);
      picked.removeLast();
    }
  }

  visit(0, <T>[]);
  return result;
}

class DotNetRandom {
  DotNetRandom(int seed) {
    var subtraction = seed == -2147483648 ? 2147483647 : seed.abs();
    var mj = 161803398 - subtraction;
    _seedArray[55] = mj;
    var mk = 1;
    for (var i = 1; i < 55; i++) {
      final ii = (21 * i) % 55;
      _seedArray[ii] = mk;
      mk = mj - mk;
      if (mk < 0) mk += 2147483647;
      mj = _seedArray[ii];
    }
    for (var k = 1; k < 5; k++) {
      for (var i = 1; i < 56; i++) {
        _seedArray[i] -= _seedArray[1 + (i + 30) % 55];
        if (_seedArray[i] < 0) _seedArray[i] += 2147483647;
      }
    }
  }
  final List<int> _seedArray = List<int>.filled(56, 0);
  int _inext = 0;
  int _inextp = 21;
  int next(int maxValue) {
    if (++_inext >= 56) _inext = 1;
    if (++_inextp >= 56) _inextp = 1;
    var value = _seedArray[_inext] - _seedArray[_inextp];
    if (value == 2147483647) value--;
    if (value < 0) value += 2147483647;
    _seedArray[_inext] = value;
    return (value * (1.0 / 2147483647) * maxValue).floor();
  }
}

class _Card {
  const _Card(this.rank, this.suit, {this.baseRank, this.replacementSuit});
  const _Card.wild(int rank) : this(rank, '', baseRank: rank);
  final int rank;
  final String suit;
  final int? baseRank;
  final String? replacementSuit;
  bool get isWild => baseRank != null;
  String get identity =>
      isWild ? _rankLabel(baseRank!) : '${_rankLabel(rank)}$suit';
  String get label => isWild && replacementSuit != null
      ? '${_rankLabel(baseRank!)}[${_rankLabel(rank)}$replacementSuit]'
      : identity;
  _Card asRank(int value, String targetSuit) =>
      _Card(value, '', baseRank: baseRank, replacementSuit: targetSuit);
  static int compare(_Card a, _Card b) {
    final rank = a.rank.compareTo(b.rank);
    return rank != 0 ? rank : a.suit.compareTo(b.suit);
  }
}

String _rankLabel(int rank) =>
    const <int, String>{11: 'J', 12: 'Q', 13: 'K'}[rank] ?? '$rank';

class _Trick {
  _Trick(this.type, List<_Card> cards, {this.bomb = false})
    : cards = List<_Card>.of(cards)..sort(_Card.compare);
  final String type;
  final List<_Card> cards;
  final bool bomb;
  bool get isBomb => bomb;
  String get trickClass {
    if (type == 'BOMB' || type == 'PASS') return 'else';
    if (type == 'SINGLE' ||
        type == 'PAIR' ||
        type == 'TRIPLE' ||
        type == 'QUAD' ||
        type == 'FIVED' ||
        type == 'SIXED') {
      return 'same';
    }
    if (type.startsWith('SEQ')) return 'sequence';
    if (type.startsWith('PAIRSEQ')) return 'pairs';
    if (type.startsWith('TRIPLESTAIR')) return 'triples';
    if (type.startsWith('QUADSTAIR')) return 'quads';
    return 'fiveds';
  }

  String get description =>
      '$type[${cards.map((card) => card.label).join('|')}]';
  int compareTo(_Trick other) {
    if (bomb != other.bomb) return bomb ? 1 : -1;
    if (bomb) return _bombRank.compareTo(other._bombRank);
    final tc = _typeValue.compareTo(other._typeValue);
    return tc != 0 ? tc : cards.first.rank.compareTo(other.cards.first.rank);
  }

  int get _bombRank {
    if (cards.length == 4) {
      return cards.map((c) => c.suit).toSet().length == 4 ? 0 : 5;
    }
    return cards.map((c) => c.baseRank).fold(0, (a, b) => a + (b ?? 0));
  }

  int get _typeValue {
    if (type == 'SINGLE') return 10;
    if (type == 'PAIR') return 20;
    if (type == 'TRIPLE') return 30;
    if (type == 'QUAD') return 40;
    if (type == 'FIVED') return 50;
    if (type == 'SIXED') return 60;
    if (type.startsWith('SEQ')) return int.parse(type.substring(3)) * 10 + 2;
    final length = int.parse(type.substring(type.length - 1));
    final size = type.startsWith('PAIR')
        ? 2
        : type.startsWith('TRIPLE')
        ? 3
        : type.startsWith('QUAD')
        ? 4
        : 5;
    return length * 20 + size * 2;
  }
}

class _Action {
  _Action(this.playerId, this.trick) : isPass = false;
  _Action.pass(this.playerId) : trick = null, isPass = true;
  final String playerId;
  final _Trick? trick;
  final bool isPass;
  String get description => isPass ? 'Pass' : trick!.description;
  Map<String, Object?> toMap() => <String, Object?>{
    'description': description,
    'pass': isPass,
    'cards': trick?.cards.length ?? 0,
    'rank': trick?.cards.first.rank ?? 0,
    'bomb': trick?.isBomb ?? false,
    'type': trick?.type ?? 'PASS',
    'class': trick?.trickClass ?? 'else',
    'wilds': trick?.cards.where((card) => card.isWild).length ?? 0,
    'identities':
        trick?.cards
            .where((card) => !card.isWild)
            .map((card) => card.identity)
            .toList() ??
        const <String>[],
  };
}

class _Move {
  _Move(this.playerId, this.action, {this.finalMove = false});
  final String playerId;
  final _Action action;
  final bool finalMove;
  String get description =>
      finalMove ? 'Final ${action.description}' : action.description;
}

class _Player {
  _Player(this.id);
  final String id;
  final List<_Card> hand = <_Card>[];
  final List<_Card> discard = <_Card>[];
  int opponentsRemaining = -1;
}

class _Deal {
  const _Deal(this.hands, this.haggis);
  final List<List<_Card>> hands;
  final List<_Card> haggis;
}

extension<T> on Iterable<T> {
  T? get firstOrNull {
    final iterator = this.iterator;
    return iterator.moveNext() ? iterator.current : null;
  }
}
