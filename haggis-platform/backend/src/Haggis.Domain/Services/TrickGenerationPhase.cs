namespace Haggis.Domain.Services
{
    public enum TrickGenerationPhase
    {
        HandIndex,
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
