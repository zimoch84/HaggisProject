import 'heuristic_models.dart';

/// Port of RedundantWildAssignmentTrickFilter.
List<HeuristicTrick> filterRedundantWildAssignments(
  List<HeuristicTrick> tricks,
) {
  final groups = <String, List<HeuristicTrick>>{};
  for (final trick in tricks) {
    groups
        .putIfAbsent(_comparableKey(trick), () => <HeuristicTrick>[])
        .add(trick);
  }
  return groups.values.expand(_filterGroup).toList(growable: false);
}

String _comparableKey(HeuristicTrick trick) {
  final includeSuit = trick.trickClass != 'same' && trick.trickClass != 'else';
  final cards =
      trick.cards
          .map(
            (card) =>
                (card.rank, includeSuit ? card.effectiveSuit : -2147483648),
          )
          .toList()
        ..sort((left, right) {
          final rank = left.$1.compareTo(right.$1);
          return rank != 0 ? rank : left.$2.compareTo(right.$2);
        });
  return '${trick.type}|${cards.map((card) => '${card.$1}:${card.$2}').join('|')}';
}

Iterable<HeuristicTrick> _filterGroup(List<HeuristicTrick> candidates) {
  final minimumWildCount = candidates
      .map((trick) => trick.wildCount)
      .reduce((left, right) => left < right ? left : right);
  var selected = candidates
      .where((trick) => trick.wildCount == minimumWildCount)
      .toList();
  if (minimumWildCount == 0) return selected;

  selected.sort(
    (left, right) => _compareInts(_wildBaseRanks(left), _wildBaseRanks(right)),
  );
  final bestRanks = _wildBaseRanks(selected.first);
  selected = selected
      .where((trick) => _compareInts(_wildBaseRanks(trick), bestRanks) == 0)
      .toList();
  selected.sort(
    (left, right) => _compareAssignments(
      _replacementAssignments(left),
      _replacementAssignments(right),
    ),
  );
  final bestAssignments = _replacementAssignments(selected.first);
  return selected.where(
    (trick) =>
        _compareAssignments(_replacementAssignments(trick), bestAssignments) ==
        0,
  );
}

List<int> _wildBaseRanks(HeuristicTrick trick) =>
    trick.cards
        .where((card) => card.isWild)
        .map((card) => card.baseRank)
        .toList()
      ..sort();

List<(int, int)> _replacementAssignments(HeuristicTrick trick) =>
    trick.cards
        .where((card) => card.isWild && card.rank != card.baseRank)
        .map((card) => (card.rank, card.baseRank))
        .toList()
      ..sort((left, right) {
        final rank = left.$1.compareTo(right.$1);
        return rank != 0 ? rank : left.$2.compareTo(right.$2);
      });

int _compareInts(List<int> left, List<int> right) {
  for (var index = 0; index < left.length && index < right.length; index++) {
    final comparison = left[index].compareTo(right[index]);
    if (comparison != 0) return comparison;
  }
  return left.length.compareTo(right.length);
}

int _compareAssignments(List<(int, int)> left, List<(int, int)> right) {
  for (var index = 0; index < left.length && index < right.length; index++) {
    var comparison = left[index].$1.compareTo(right[index].$1);
    if (comparison != 0) return comparison;
    comparison = left[index].$2.compareTo(right[index].$2);
    if (comparison != 0) return comparison;
  }
  return left.length.compareTo(right.length);
}
