namespace Haggis.AI.Benchmark
{
    public sealed class HeuristicGeneticTuningOptions
    {
        public int Games { get; set; } = 100;
        public int SeedStart { get; set; } = 1;
        public bool Rotate { get; set; } = true;
        public int GameOverScore { get; set; } = 250;
        public int MaxMovesPerGame { get; set; } = 10000;
        public string MonteCarloStrategy { get; set; } = "montecarlo:2000:2000:4";
        public string CsvPath { get; set; }
        public int Generations { get; set; } = 5;
        public int ChildrenPerGeneration { get; set; } = 20;
        public int TopParentCount { get; set; } = 10;
        public float MutationRate { get; set; } = 0.35f;
        public float MutationDeltaMin { get; set; } = -3f;
        public float MutationDeltaMax { get; set; } = 3f;
        public int RandomSeed { get; set; } = 12345;
        public float BaselineWeight { get; set; } = 1f;
    }
}
