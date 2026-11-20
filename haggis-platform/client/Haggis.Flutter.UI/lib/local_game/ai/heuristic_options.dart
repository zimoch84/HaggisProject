/// Exact Dart counterpart of Haggis.AI.Strategies.HeuristicOptions.
class HeuristicOptions {
  const HeuristicOptions({
    this.preferSinglesNotBreakingNonWildCombinationsWeight = 1,
    this.continuationCountWeight = 2.455,
    this.preferNonBreakableOpeningWeight = 0,
    this.preferLowerStartWeight = 1.983,
    this.preferShorterStartWeight = 1.686,
    this.wildCardOpeningWeight = 4.418,
    this.bombOpeningWeight = 0.608,
    this.higherRelatedCombinationOpeningWeight = 3.458,
    this.wildCardContinuationWeight = 0,
    this.bombContinuationWeight = 3.646,
    this.higherRelatedCombinationContinuationWeight = 0.224,
    this.preferUsingWildAsHigherCardInContinuationWeight = 1.528,
    this.lowerValueContinuationWeight = 3.652,
    this.continuationFollowUpWeight = 1,
    this.playableBombInEndgameWeight = 0,
  });

  final double preferSinglesNotBreakingNonWildCombinationsWeight;
  final double continuationCountWeight;
  final double preferNonBreakableOpeningWeight;
  final double preferLowerStartWeight;
  final double preferShorterStartWeight;
  final double wildCardOpeningWeight;
  final double bombOpeningWeight;
  final double higherRelatedCombinationOpeningWeight;
  final double wildCardContinuationWeight;
  final double bombContinuationWeight;
  final double higherRelatedCombinationContinuationWeight;
  final double preferUsingWildAsHigherCardInContinuationWeight;
  final double lowerValueContinuationWeight;
  final double continuationFollowUpWeight;
  final double playableBombInEndgameWeight;
}
