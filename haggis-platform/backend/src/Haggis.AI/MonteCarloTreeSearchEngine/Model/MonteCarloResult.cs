using Haggis.Domain.Interfaces;
using MonteCarlo;
using System.Collections.Generic;

namespace Haggis.AI.Model
{
    public class MonteCarloResult
    {
        public IHaggisPlayer Player { get; set; }
        public List<MonteCarloActionInfo> Actions { get; set; }
        public int Iterations { get; set; }
        public long BudgetMs { get; set; }
        public long ElapsedMs { get; set; }
        public int LegalActionsCount { get; set; }
        public int RootChildrenCount { get; set; }
        public int Workers { get; set; }
        public int ScheduledRollouts { get; set; }
        public int CompletedRollouts { get; set; }
        public int TreeNodeCount { get; set; }
        public int TreeMaxDepth { get; set; }
        public MctsTimingResult Timing { get; set; }
    }
}
