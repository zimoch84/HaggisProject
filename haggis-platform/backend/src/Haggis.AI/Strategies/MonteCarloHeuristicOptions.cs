namespace Haggis.AI.Strategies
{
    public sealed class MonteCarloHeuristicOptions
    {
        public bool Enabled { get; set; }
        public int TreeTopN { get; set; }
        public int RolloutTopN { get; set; }
        public HeuristicOptions HeuristicOptions { get; set; } = new HeuristicOptions();
    }
}
