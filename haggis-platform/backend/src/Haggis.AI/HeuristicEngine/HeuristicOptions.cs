namespace Haggis.AI.Strategies
{
    public sealed class HeuristicOptions
    {
        // All options below are external multipliers for strategy base scores.
        // 0 disables a strategy, 1 keeps its built-in default strength, values
        // above 1 strengthen its effect proportionally.

        // PreferSinglesNotBreakingNonWildCombinationsWeightStrategy
        public float PreferSinglesNotBreakingNonWildCombinationsWeight { get; set; } = 1f;

        // PreferTricksWithMoreContinuationsWeightStrategy
        public float ContinuationCountWeight { get; set; } = 2.455f;

        // PreferTricksThatAreMostLikelyNonBreakableWeightStrategy
        public float PreferNonBreakableOpeningWeight { get; set; } = 0f;

        // PreferLowerTricksWhenHandIsLargeWeightStrategy
        public float PreferLowerStartWeight { get; set; } = 1.983f;

        // PreferShorterTricksWhenHandIsLargeWeightStrategy
        public float PreferShorterStartWeight { get; set; } = 1.686f;

        // PenalizeWildCardsInOpeningWeightStrategy
        public float WildCardOpeningWeight { get; set; } = 4.418f;

        // PenalizeBombOpeningWeightStrategy
        public float BombOpeningWeight { get; set; } = 0.608f;

        // PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy
        public float HigherRelatedCombinationOpeningWeight { get; set; } = 3.458f;

        // PenalizeWildCardsInContinuationWeightStrategy
        public float WildCardContinuationWeight { get; set; } = 0f;

        // PenalizeBombContinuationWeightStrategy
        public float BombContinuationWeight { get; set; } = 3.646f;

        // PenalizeContinuationWhenHigherRelatedCombinationExistsWeightStrategy
        public float HigherRelatedCombinationContinuationWeight { get; set; } = 0.224f;

        // PreferUsingWildAsHigherCardInContinuationWeightStrategy
        public float PreferUsingWildAsHigherCardInContinuationWeight { get; set; } = 1.528f;

        // PreferLowerValueContinuationWeightStrategy
        public float LowerValueContinuationWeight { get; set; } = 3.652f;

        // PreferContinuationsWithFollowUpWeightStrategy
        public float ContinuationFollowUpWeight { get; set; } = 1f;

        // PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy
        public float PlayableBombInEndgameWeight { get; set; } = 0f;
    }
}
