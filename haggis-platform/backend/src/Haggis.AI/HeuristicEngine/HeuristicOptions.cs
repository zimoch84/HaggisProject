namespace Haggis.AI.Strategies
{
    public sealed class HeuristicOptions
    {
        // All options below are external multipliers for strategy base scores.
        // 0 disables a strategy, 1 keeps its built-in default strength, values
        // above 1 strengthen its effect proportionally.

        // PreferSinglesNotBreakingNonWildCombinationsWeightStrategy
        public float PreferSinglesNotBreakingNonWildCombinationsWeight { get; set; } = 2.1f;

        // PreferTricksWithMoreContinuationsWeightStrategy
        public float ContinuationCountWeight { get; set; } = 3.68f;

        // PreferTricksThatAreMostLikelyNonBreakableWeightStrategy
        public float PreferNonBreakableOpeningWeight { get; set; } = 0.76f;

        // PreferLowerTricksWhenHandIsLargeWeightStrategy
        public float PreferLowerStartWeight { get; set; } = 3.5f;

        // PreferShorterTricksWhenHandIsLargeWeightStrategy
        public float PreferShorterStartWeight { get; set; } = 1f;

        // PenalizeWildCardsInOpeningWeightStrategy
        public float WildCardOpeningWeight { get; set; } = 2.4f;

        // PenalizeBombOpeningWeightStrategy
        public float BombOpeningWeight { get; set; } = 1f;

        // PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy
        public float HigherRelatedCombinationOpeningWeight { get; set; } = 3f;

        // PenalizeWildCardsInContinuationWeightStrategy
        public float WildCardContinuationWeight { get; set; } = 0f;

        // PenalizeBombContinuationWeightStrategy
        public float BombContinuationWeight { get; set; } = 6f;

        // PenalizeContinuationWhenHigherRelatedCombinationExistsWeightStrategy
        public float HigherRelatedCombinationContinuationWeight { get; set; } = 1f;

        // PreferUsingWildAsHigherCardInContinuationWeightStrategy
        public float PreferUsingWildAsHigherCardInContinuationWeight { get; set; } = 1.3f;

        // PreferLowerValueContinuationWeightStrategy
        public float LowerValueContinuationWeight { get; set; } = 0f;

        // PreferContinuationsWithFollowUpWeightStrategy
        public float ContinuationFollowUpWeight { get; set; } = 1.17f;

        // PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy
        public float PlayableBombInEndgameWeight { get; set; } = 0f;
    }
}
