using System;
using Haggis.AI.Strategies;

namespace Haggis.AI.Benchmark
{
    public sealed class HeuristicTuningRunResult
    {
        public int Games { get; set; }
        public int SeedStart { get; set; }
        public bool Rotate { get; set; }
        public int GameOverScore { get; set; }
        public int MaxMovesPerGame { get; set; }
        public string MonteCarloStrategy { get; set; }
        public HeuristicOptions HeuristicOptions { get; set; }
        public int CompletedGames { get; set; }
        public int FailedGames { get; set; }
        public int HeuristicWins { get; set; }
        public int MonteCarloWins { get; set; }
        public double HeuristicWinRatePct { get; set; }
        public double MonteCarloWinRatePct { get; set; }
        public double HeuristicAverageScore { get; set; }
        public double MonteCarloAverageScore { get; set; }
        public double AverageRounds { get; set; }
        public double AverageMoves { get; set; }
        public double AverageGameElapsedMs { get; set; }
        public long TotalGameElapsedMs { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
