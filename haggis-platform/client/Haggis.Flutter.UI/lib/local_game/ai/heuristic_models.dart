class HeuristicCard {
  const HeuristicCard({
    required this.rank,
    required this.baseRank,
    required this.suit,
    required this.effectiveSuit,
    required this.label,
  });

  factory HeuristicCard.fromMap(Map<Object?, Object?> map) => HeuristicCard(
    rank: map['rank']! as int,
    baseRank: map['baseRank']! as int,
    suit: map['suit']! as int,
    effectiveSuit: map['effectiveSuit']! as int,
    label: map['label']! as String,
  );

  final int rank;
  final int baseRank;
  final int suit;
  final int effectiveSuit;
  final String label;
  bool get isWild => baseRank >= 11;

  String get identity => '$baseRank:$suit';
}

class HeuristicTrick implements Comparable<HeuristicTrick> {
  HeuristicTrick({
    required this.type,
    required this.description,
    required this.typeValue,
    required this.bombRank,
    required this.trickClass,
    required this.cards,
  });

  factory HeuristicTrick.fromMap(Map<Object?, Object?> map) => HeuristicTrick(
    type: map['type']! as String,
    description: map['description']! as String,
    typeValue: map['typeValue']! as int,
    bombRank: map['bombRank']! as int,
    trickClass: map['class']! as String,
    cards: (map['cardData']! as List<Object?>)
        .cast<Map<Object?, Object?>>()
        .map(HeuristicCard.fromMap)
        .toList(growable: false),
  );

  final String type;
  final String description;
  final int typeValue;
  final int bombRank;
  final String trickClass;
  final List<HeuristicCard> cards;
  bool get isBomb => type == 'BOMB';
  int get wildCount => cards.where((card) => card.isWild).length;

  @override
  int compareTo(HeuristicTrick other) {
    if (isBomb != other.isBomb) return isBomb ? 1 : -1;
    if (isBomb) return bombRank.compareTo(other.bombRank);
    final typeComparison = typeValue.compareTo(other.typeValue);
    return typeComparison != 0
        ? typeComparison
        : cards.first.rank.compareTo(other.cards.first.rank);
  }

  String get equalityKey =>
      '$type|${cards.map((card) => card.identity).join('|')}';
}

class HeuristicAction {
  HeuristicAction({
    required this.originalIndex,
    required this.description,
    required this.isPass,
    required this.trick,
  });

  factory HeuristicAction.fromMap(Map<Object?, Object?> map, int index) =>
      HeuristicAction(
        originalIndex: index,
        description: map['description']! as String,
        isPass: map['pass']! as bool,
        trick: map['pass'] == true ? null : HeuristicTrick.fromMap(map),
      );

  final int originalIndex;
  final String description;
  final bool isPass;
  final HeuristicTrick? trick;
}

class HeuristicContext {
  const HeuristicContext({
    required this.actions,
    required this.hand,
    required this.playersDiscard,
    required this.currentTrickCards,
    required this.opening,
  });

  factory HeuristicContext.fromInput(Map<String, Object?> input) {
    final rawActions = (input['actions']! as List<Object?>)
        .cast<Map<Object?, Object?>>();
    return HeuristicContext(
      actions: <HeuristicAction>[
        for (var index = 0; index < rawActions.length; index++)
          HeuristicAction.fromMap(rawActions[index], index),
      ],
      hand: (input['hand']! as List<Object?>)
          .cast<Map<Object?, Object?>>()
          .map(HeuristicCard.fromMap)
          .toList(growable: false),
      playersDiscard: (input['discard']! as List<Object?>)
          .cast<Map<Object?, Object?>>()
          .map(HeuristicCard.fromMap)
          .toList(growable: false),
      currentTrickCards: (input['currentTrickCards']! as List<Object?>)
          .cast<Map<Object?, Object?>>()
          .map(HeuristicCard.fromMap)
          .toList(growable: false),
      opening: input['opening']! as bool,
    );
  }

  final List<HeuristicAction> actions;
  final List<HeuristicCard> hand;
  final List<HeuristicCard> playersDiscard;
  final List<HeuristicCard> currentTrickCards;
  final bool opening;
}

class HeuristicRankedAction {
  const HeuristicRankedAction({
    required this.action,
    required this.weight,
    required this.breakdown,
  });

  final HeuristicAction action;
  final int weight;
  final Map<String, int> breakdown;
}
