import 'heuristic_models.dart';
import 'heuristic_options.dart';
import 'heuristic_weight_normalization.dart';
import 'redundant_wild_assignment_filter.dart';

class HeuristicActionRanker {
  const HeuristicActionRanker({this.options = const HeuristicOptions()});

  final HeuristicOptions options;

  List<HeuristicRankedAction> rank(HeuristicContext context) =>
      context.opening ? _rankOpening(context) : _rankContinuation(context);

  String choose(HeuristicContext context) {
    final ranked = rank(context);
    return ranked.isEmpty
        ? context.actions.first.description
        : ranked.first.action.description;
  }

  List<HeuristicRankedAction> _rankOpening(HeuristicContext context) {
    final playable = context.actions.where((action) => !action.isPass).toList();
    if (playable.isEmpty) return const <HeuristicRankedAction>[];
    final tricks = playable.map((action) => action.trick!).toList();
    final ranked = <HeuristicRankedAction>[
      for (final action in playable)
        _ranked(action, _openingWeights(action.trick!, tricks, context)),
    ];
    ranked.sort((left, right) {
      final weight = right.weight.compareTo(left.weight);
      return weight != 0
          ? weight
          : left.action.originalIndex.compareTo(right.action.originalIndex);
    });
    return ranked;
  }

  List<HeuristicRankedAction> _rankContinuation(HeuristicContext context) {
    final pass = context.actions.where((action) => action.isPass).firstOrNull;
    final playable = context.actions.where((action) => !action.isPass).toList();
    if (playable.isEmpty) return const <HeuristicRankedAction>[];
    if (playable.length == 1 && pass == null) {
      return <HeuristicRankedAction>[
        HeuristicRankedAction(
          action: playable.first,
          weight: 0,
          breakdown: const <String, int>{
            'auto-selected-single-legal-action': 0,
          },
        ),
      ];
    }

    final distinct = <String, HeuristicTrick>{};
    for (final action in playable) {
      distinct.putIfAbsent(action.trick!.equalityKey, () => action.trick!);
    }
    final filtered = filterRedundantWildAssignments(distinct.values.toList());
    final filteredDescriptions = filtered
        .map((trick) => trick.description)
        .toSet();
    final candidates = playable
        .where((action) => filteredDescriptions.contains(action.description))
        .toList();
    final allTricks = playable.map((action) => action.trick!).toList();
    final ranked = <HeuristicRankedAction>[
      for (final action in candidates)
        _ranked(
          action,
          _continuationWeights(action.trick!, allTricks, context),
        ),
    ];
    final bestWeight = ranked.isEmpty
        ? -0x80000000
        : ranked
              .map((candidate) => candidate.weight)
              .reduce((left, right) => left > right ? left : right);
    if (pass != null && bestWeight <= 0) {
      ranked.add(
        HeuristicRankedAction(
          action: pass,
          weight: 0,
          breakdown: const <String, int>{},
        ),
      );
    }
    ranked.sort(_compareContinuationCandidates);
    return ranked;
  }

  HeuristicRankedAction _ranked(
    HeuristicAction action,
    Map<String, int> breakdown,
  ) => HeuristicRankedAction(
    action: action,
    weight: breakdown.values.fold(0, (sum, weight) => sum + weight),
    breakdown: breakdown,
  );

  Map<String, int> _openingWeights(
    HeuristicTrick trick,
    List<HeuristicTrick> tricks,
    HeuristicContext context,
  ) => <String, int>{
    'PreferTricksThatAreMostLikelyNonBreakableWeightStrategy': _nonBreakable(
      trick,
      context,
    ),
    'PreferLowerTricksWhenHandIsLargeWeightStrategy': _preferLowerOpening(
      trick,
      context,
    ),
    'PreferShorterTricksWhenHandIsLargeWeightStrategy': _preferShorterOpening(
      trick,
      context,
    ),
    'PenalizeBombOpeningWeightStrategy': _penalizeBombOpening(trick, context),
    'PenalizeWildCardsInOpeningWeightStrategy': _penalizeWildOpening(
      trick,
      tricks,
      context,
    ),
    'PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy':
        _penalizeHigherRelatedOpening(trick, tricks, context),
    'PreferTricksWithMoreContinuationsWeightStrategy':
        _preferContinuationCountOpening(trick, tricks),
    'PreferSinglesNotBreakingNonWildCombinationsWeightStrategy':
        _preferSafeSingle(trick, tricks),
  };

  Map<String, int> _continuationWeights(
    HeuristicTrick trick,
    List<HeuristicTrick> tricks,
    HeuristicContext context,
  ) => <String, int>{
    'PenalizeBombContinuationWeightStrategy': trick.isBomb
        ? _apply(-40, options.bombContinuationWeight)
        : 0,
    'PenalizeWildCardsInContinuationWeightStrategy': _penalizeWildContinuation(
      trick,
      context,
    ),
    'PenalizeContinuationWhenHigherRelatedCombinationExistsWeightStrategy':
        _penalizeHigherRelatedContinuation(trick, tricks, context),
    'PreferUsingWildAsHigherCardInContinuationWeightStrategy':
        _preferWildAsHigher(trick, tricks),
    'PreferLowerValueContinuationWeightStrategy': _preferLowerContinuation(
      trick,
      tricks,
    ),
    'PreferContinuationsWithFollowUpWeightStrategy': _preferFollowUp(
      trick,
      tricks,
    ),
    'PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy':
        _preferBombEndgame(trick, context),
  };

  int _nonBreakable(HeuristicTrick trick, HeuristicContext context) {
    if (options.preferNonBreakableOpeningWeight <= 0 ||
        trick.cards.isEmpty ||
        trick.trickClass != 'same') {
      return 0;
    }
    final unavailable = <int, int>{};
    for (final card in <HeuristicCard>[
      ...context.playersDiscard,
      ...context.currentTrickCards,
    ]) {
      unavailable[card.baseRank] = (unavailable[card.baseRank] ?? 0) + 1;
    }
    final unavailableWilds =
        (unavailable[11] ?? 0) +
        (unavailable[12] ?? 0) +
        (unavailable[13] ?? 0);
    final remainingWilds = (3 - unavailableWilds).clamp(0, 3);
    final baseRank = trick.cards.first.rank;
    if (baseRank >= 13) {
      return _apply(25, options.preferNonBreakableOpeningWeight);
    }
    var exhausted = 0;
    for (var rank = baseRank + 1; rank <= 13; rank++) {
      final total = rank <= 10 ? 5 : 1;
      final natural = (total - (unavailable[rank] ?? 0)).clamp(0, total);
      if (natural + remainingWilds < trick.cards.length) exhausted++;
    }
    final probability = exhausted / (13 - baseRank);
    return probability >= 1
        ? _apply(25, options.preferNonBreakableOpeningWeight)
        : 0;
  }

  int _preferLowerOpening(HeuristicTrick trick, HeuristicContext context) {
    if (options.preferLowerStartWeight <= 0 || trick.cards.isEmpty) return 0;
    final phaseBias = HeuristicWeightNormalization.clamp(
      (context.hand.length - 8) / 8,
      -1,
      1,
    );
    final averageRank =
        trick.cards.map((card) => card.rank).reduce((a, b) => a + b) /
        trick.cards.length;
    final rankBias = 1 - (2 * (averageRank - 2) / 11);
    return _apply(
      _round(10 * phaseBias * rankBias),
      options.preferLowerStartWeight,
    );
  }

  int _preferShorterOpening(HeuristicTrick trick, HeuristicContext context) {
    final handCount = context.hand.length;
    if (options.preferShorterStartWeight <= 0 ||
        handCount <= 0 ||
        trick.cards.isEmpty) {
      return 0;
    }
    final phase = HeuristicWeightNormalization.clamp((handCount - 8) / 8, 0, 1);
    final shorter =
        1 -
        HeuristicWeightNormalization.normalizeRatio(
          trick.cards.length - 1,
          handCount - 1,
        );
    return _apply(_base(phase * shorter, 24), options.preferShorterStartWeight);
  }

  int _penalizeBombOpening(HeuristicTrick trick, HeuristicContext context) =>
      trick.isBomb
      ? _apply(
          _base(
            -HeuristicWeightNormalization.handPhase(context.hand.length),
            100,
          ),
          options.bombOpeningWeight,
        )
      : 0;

  int _penalizeWildOpening(
    HeuristicTrick trick,
    List<HeuristicTrick> tricks,
    HeuristicContext context,
  ) {
    if (options.wildCardOpeningWeight <= 0 || trick.wildCount == 0) return 0;
    var signal =
        HeuristicWeightNormalization.normalizeRatio(
          trick.wildCount,
          trick.cards.length,
        ) *
        HeuristicWeightNormalization.handPhase(context.hand.length);
    if (_hasEquivalentWithLowerWilds(trick, tricks)) {
      signal = HeuristicWeightNormalization.clamp(signal * 2, 0, 1);
    }
    return _apply(_base(-signal, 50), options.wildCardOpeningWeight);
  }

  bool _hasEquivalentWithLowerWilds(
    HeuristicTrick trick,
    List<HeuristicTrick> tricks,
  ) {
    final currentWildRanks = _wildBaseRanks(trick);
    if (currentWildRanks.isEmpty) return false;
    final effectiveRanks = trick.cards.map((card) => card.rank).toList()
      ..sort();
    final nonWild =
        trick.cards
            .where((card) => !card.isWild)
            .map((card) => card.label)
            .toList()
          ..sort();
    return tricks.where((candidate) => !identical(candidate, trick)).any((
      candidate,
    ) {
      final candidateRanks = candidate.cards.map((card) => card.rank).toList()
        ..sort();
      final candidateNatural =
          candidate.cards
              .where((card) => !card.isWild)
              .map((card) => card.label)
              .toList()
            ..sort();
      return candidate.type == trick.type &&
          candidate.cards.length == trick.cards.length &&
          candidate.wildCount == currentWildRanks.length &&
          _listEquals(candidateRanks, effectiveRanks) &&
          _listEquals(candidateNatural, nonWild) &&
          _compareInts(_wildBaseRanks(candidate), currentWildRanks) < 0;
    });
  }

  int _penalizeHigherRelatedOpening(
    HeuristicTrick trick,
    List<HeuristicTrick> tricks,
    HeuristicContext context,
  ) {
    if (options.higherRelatedCombinationOpeningWeight <= 0 ||
        trick.trickClass == 'else') {
      return 0;
    }
    final related = tricks
        .where((candidate) => !identical(candidate, trick))
        .any(
          (candidate) =>
              candidate.trickClass == trick.trickClass &&
              candidate.cards.length > trick.cards.length &&
              candidate.cards.every((card) => !card.isWild) &&
              trick.cards.every((card) => _containsCard(candidate.cards, card)),
        );
    return related
        ? _apply(
            _base(
              -HeuristicWeightNormalization.handPhase(context.hand.length),
              50,
            ),
            options.higherRelatedCombinationOpeningWeight,
          )
        : 0;
  }

  int _preferContinuationCountOpening(
    HeuristicTrick trick,
    List<HeuristicTrick> tricks,
  ) {
    if (options.continuationCountWeight <= 0 || trick.type == 'SINGLE') {
      return 0;
    }
    final counts = <HeuristicTrick, int>{
      for (final candidate in tricks.where(
        (candidate) => candidate.type != 'SINGLE',
      ))
        candidate: _countContinuations(tricks, candidate),
    };
    final maximum = counts.values.fold(
      0,
      (max, value) => value > max ? value : max,
    );
    if (maximum <= 0) return 0;
    return _apply(
      _base(
        HeuristicWeightNormalization.normalizeRatio(
          counts[trick] ?? 0,
          maximum,
        ),
        20,
      ),
      options.continuationCountWeight,
    );
  }

  int _preferSafeSingle(HeuristicTrick trick, List<HeuristicTrick> tricks) {
    if (options.preferSinglesNotBreakingNonWildCombinationsWeight <= 0 ||
        trick.type != 'SINGLE' ||
        trick.cards.length != 1 ||
        trick.cards.first.isWild) {
      return 0;
    }
    final card = trick.cards.first;
    final breaks = tricks.any(
      (candidate) =>
          candidate.type != 'SINGLE' &&
          candidate.cards.length > 1 &&
          candidate.cards.every((candidateCard) => !candidateCard.isWild) &&
          _containsCard(candidate.cards, card),
    );
    return breaks
        ? 0
        : _apply(40, options.preferSinglesNotBreakingNonWildCombinationsWeight);
  }

  int _penalizeWildContinuation(
    HeuristicTrick trick,
    HeuristicContext context,
  ) {
    if (options.wildCardContinuationWeight <= 0 || trick.wildCount == 0) {
      return 0;
    }
    final signal =
        HeuristicWeightNormalization.normalizeRatio(
          trick.wildCount,
          trick.cards.length,
        ) *
        HeuristicWeightNormalization.handPhase(context.hand.length);
    return _apply(_base(-signal, 50), options.wildCardContinuationWeight);
  }

  int _penalizeHigherRelatedContinuation(
    HeuristicTrick trick,
    List<HeuristicTrick> tricks,
    HeuristicContext context,
  ) {
    if (options.higherRelatedCombinationContinuationWeight <= 0 ||
        trick.trickClass == 'else') {
      return 0;
    }
    final natural = trick.cards.where((card) => !card.isWild).toList();
    if (natural.isEmpty) return 0;
    final related = tricks
        .where((candidate) => !identical(candidate, trick))
        .any(
          (candidate) =>
              candidate.trickClass == trick.trickClass &&
              candidate.cards.length > trick.cards.length &&
              natural.every((card) => _containsCard(candidate.cards, card)),
        );
    return related
        ? _apply(
            _base(
              -HeuristicWeightNormalization.handPhase(context.hand.length),
              40,
            ),
            options.higherRelatedCombinationContinuationWeight,
          )
        : 0;
  }

  int _preferWildAsHigher(HeuristicTrick trick, List<HeuristicTrick> tricks) {
    if (options.preferUsingWildAsHigherCardInContinuationWeight <= 0) return 0;
    final current = _effectiveWildRanks(trick);
    if (current.isEmpty) return 0;
    final alternatives = tricks
        .where(
          (candidate) =>
              !identical(candidate, trick) &&
              candidate.type == trick.type &&
              candidate.cards.length == trick.cards.length &&
              candidate.wildCount == current.length &&
              candidate.compareTo(trick) > 0,
        )
        .map(_effectiveWildRanks)
        .where((ranks) => _compareInts(ranks, current) > 0)
        .toList();
    if (alternatives.isEmpty) return 0;
    alternatives.sort(_compareInts);
    final selectedImprovement = _rankImprovement(current, alternatives.last);
    final maxImprovement = alternatives
        .map((ranks) => _rankImprovement(current, ranks))
        .reduce((left, right) => left > right ? left : right);
    if (selectedImprovement <= 0) return 0;
    return _apply(
      _base(
        -HeuristicWeightNormalization.normalizeRatio(
          selectedImprovement,
          maxImprovement,
        ),
        30,
      ),
      options.preferUsingWildAsHigherCardInContinuationWeight,
    );
  }

  int _preferLowerContinuation(
    HeuristicTrick trick,
    List<HeuristicTrick> tricks,
  ) {
    if (options.lowerValueContinuationWeight <= 0 || trick.isBomb) return 0;
    final ranks =
        tricks
            .where((candidate) => candidate.type == trick.type)
            .map((candidate) => candidate.cards.first.rank)
            .toSet()
            .toList()
          ..sort();
    if (ranks.length <= 1) return 0;
    final opportunity =
        ranks.length - 1 - ranks.indexOf(trick.cards.first.rank);
    if (opportunity <= 0) return 0;
    return _apply(
      _base(
        HeuristicWeightNormalization.normalizeRatio(
          opportunity,
          ranks.length - 1,
        ),
        20,
      ),
      options.lowerValueContinuationWeight,
    );
  }

  int _preferFollowUp(HeuristicTrick trick, List<HeuristicTrick> tricks) {
    if (options.continuationFollowUpWeight <= 0) return 0;
    final counts = <HeuristicTrick, int>{
      for (final candidate in tricks)
        candidate: _countContinuations(tricks, candidate),
    };
    final maximum = counts.values.fold(
      0,
      (max, value) => value > max ? value : max,
    );
    if (maximum <= 0) return 0;
    return _apply(
      _base(
        HeuristicWeightNormalization.normalizeRatio(
          counts[trick] ?? 0,
          maximum,
        ),
        50,
      ),
      options.continuationFollowUpWeight,
    );
  }

  int _preferBombEndgame(HeuristicTrick trick, HeuristicContext context) {
    if (options.playableBombInEndgameWeight <= 0 ||
        context.hand.length > 7 ||
        !context.actions.any((action) => action.isPass) ||
        !trick.isBomb) {
      return 0;
    }
    return _apply(100, options.playableBombInEndgameWeight);
  }

  int _countContinuations(
    List<HeuristicTrick> tricks,
    HeuristicTrick compared,
  ) {
    final highest = compared.cards.last;
    return tricks
        .where(
          (trick) =>
              trick.wildCount == 0 &&
              trick.type == compared.type &&
              trick.compareTo(compared) == 1 &&
              !_containsCard(trick.cards, highest),
        )
        .length;
  }

  int _compareContinuationCandidates(
    HeuristicRankedAction left,
    HeuristicRankedAction right,
  ) {
    var comparison = right.weight.compareTo(left.weight);
    if (comparison != 0) return comparison;
    comparison = (left.action.isPass ? 1 : 0).compareTo(
      right.action.isPass ? 1 : 0,
    );
    if (comparison != 0) return comparison;
    final leftTrick = left.action.trick;
    final rightTrick = right.action.trick;
    if (leftTrick != null && rightTrick != null) {
      comparison = leftTrick.compareTo(rightTrick);
      if (comparison != 0) return comparison;
    } else if (leftTrick == null && rightTrick != null) {
      return -1;
    } else if (leftTrick != null && rightTrick == null) {
      return 1;
    }
    comparison = left.action.description.compareTo(right.action.description);
    return comparison != 0
        ? comparison
        : left.action.originalIndex.compareTo(right.action.originalIndex);
  }

  int _base(double signal, int impact) =>
      HeuristicWeightNormalization.baseScore(signal, impact);
  int _apply(int base, double weight) =>
      HeuristicWeightNormalization.applyWeight(base, weight);
  int _round(double value) =>
      value >= 0 ? (value + 0.5).floor() : (value - 0.5).ceil();
}

bool _containsCard(List<HeuristicCard> cards, HeuristicCard target) =>
    cards.any((card) => card.identity == target.identity);

List<int> _wildBaseRanks(HeuristicTrick trick) =>
    trick.cards
        .where((card) => card.isWild)
        .map((card) => card.baseRank)
        .toList()
      ..sort();

List<int> _effectiveWildRanks(HeuristicTrick trick) =>
    trick.cards.where((card) => card.isWild).map((card) => card.rank).toList()
      ..sort();

int _rankImprovement(List<int> current, List<int> better) {
  var result = 0;
  for (
    var index = 0;
    index < current.length && index < better.length;
    index++
  ) {
    final delta = better[index] - current[index];
    if (delta > 0) result += delta;
  }
  return result;
}

int _compareInts(List<int> left, List<int> right) {
  for (var index = 0; index < left.length && index < right.length; index++) {
    final comparison = left[index].compareTo(right[index]);
    if (comparison != 0) return comparison;
  }
  return left.length.compareTo(right.length);
}

bool _listEquals<T>(List<T> left, List<T> right) {
  if (left.length != right.length) return false;
  for (var index = 0; index < left.length; index++) {
    if (left[index] != right[index]) return false;
  }
  return true;
}

extension<T> on Iterable<T> {
  T? get firstOrNull {
    final iterator = this.iterator;
    return iterator.moveNext() ? iterator.current : null;
  }
}
