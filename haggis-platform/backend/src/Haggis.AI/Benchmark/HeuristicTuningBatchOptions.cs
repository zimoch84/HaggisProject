using System.Collections.Generic;
using Haggis.AI.Strategies;

namespace Haggis.AI.Benchmark
{
    public sealed class HeuristicTuningBatchOptions
    {
        public float BaselineWeight { get; set; } = 1f;
        public float WeightStep { get; set; } = 0.25f;
        public int Games { get; set; } = 100;
        public int SeedStart { get; set; } = 1;
        public bool Rotate { get; set; } = true;
        public int GameOverScore { get; set; } = 250;
        public int MaxMovesPerGame { get; set; } = 10000;
        public string MonteCarloStrategy { get; set; } = "montecarlo:2000:2000:4";
        public string CsvPath { get; set; }
        public IReadOnlyList<HeuristicOptions> WeightSets { get; set; } = new List<HeuristicOptions>();
    }
}
