namespace Haggis.Domain.Services
{
    public enum TrickGenerationPhase
    {
        HandIndex,
        HandIndexRankBuckets,
        HandIndexSuitBuckets,
        HandIndexPrefixCounts,
        HandIndexSameRankCombinations,
        HandIndexSorting,
        SameCards,
        SameCardsWithWilds,
        Sequences,
        Stairs,
        Bombs,
        ContinuationFilter,
        TrickSelection,
        ActionWrapping,
        ActionSelection,
        PassAppend
    }
}
