using Haggis.Domain.Model;
using System.Collections.Generic;

namespace Haggis.AI.Strategies
{
    public sealed class HeuristicWeightBreakdown
    {
        public string StrategyName { get; set; }
        public int Weight { get; set; }
    }

    public sealed class HeuristicRankedAction
    {
        internal int OriginalIndex { get; set; }

        public HaggisAction Action { get; set; }
        public Trick Trick { get; set; }
        public IReadOnlyList<HeuristicWeightBreakdown> Breakdown { get; set; }
        public int Weight { get; set; }
        public string SortLabel { get; set; }
    }
}
