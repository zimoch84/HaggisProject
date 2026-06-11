namespace Haggis.AI.Strategies
{
    public sealed class HeuristicOptions
    {
        public int PreferSinglesNotBreakingNonWildCombinationsWeight { get; set; } = 3;
        public int ContinuationCountWeight { get; set; } = 10;
        public int PreferNonBreakableOpeningMaxWeight { get; set; } = 50;
        public int PreferLowerStartCutoff { get; set; } = 8;
        public int PreferLowerStartMaxWeight { get; set; } = 10;
        public int ShorterStartCutoff { get; set; } = 8;
        public int ShorterStartNormalization { get; set; } = 3;
        public int WildCardOpeningPenaltyFactor { get; set; } = 7;
        public int BombOpeningPenaltyFactor { get; set; } = 100;
        public int HigherRelatedCombinationOpeningPenaltyFactor { get; set; } = 6;
    }
}
