namespace Haggis.MctsRunAnalyzer
{
    public sealed class MctsRunAnalyzerOptions
    {
        public int Seed { get; set; } = 1;
        public string Ai1Strategy { get; set; } = "montecarlo";
        public string Ai2Strategy { get; set; } = "normal";
        public string Ai3Strategy { get; set; } = "normal";
        public int Iterations { get; set; } = 1000;
        public long TimeBudgetMs { get; set; } = 1000L;
        public int Workers { get; set; } = 1;
        public string OutputPath { get; set; }

        public string StrategyLabel => $"{Ai1Strategy}-vs-{Ai2Strategy}-vs-{Ai3Strategy}";
    }
}
