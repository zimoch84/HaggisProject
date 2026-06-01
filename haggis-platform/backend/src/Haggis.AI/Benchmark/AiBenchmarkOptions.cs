namespace Haggis.AI.Benchmark
{
    public sealed class AiBenchmarkOptions
    {
        public int Games { get; set; } = 1000;
        public int Players { get; set; } = 3;
        public int SeedStart { get; set; } = 1;
        public int GameOverScore { get; set; } = 250;
        public string Ai1Strategy { get; set; } = "normal";
        public string Ai2Strategy { get; set; } = "normal";
        public string Ai3Strategy { get; set; } = "normal";
        public bool Rotate { get; set; } = true;
        public string CsvPath { get; set; }
        public string LogPath { get; set; }
        public int MaxMovesPerGame { get; set; } = 10000;

        public string GetSeatStrategy(int seatNumber)
        {
            switch (seatNumber)
            {
                case 1:
                    return Ai1Strategy;
                case 2:
                    return Ai2Strategy;
                case 3:
                    return Ai3Strategy;
                default:
                    return Ai1Strategy;
            }
        }

        public string StrategyLabel => Players > 2
            ? $"{Ai1Strategy}-vs-{Ai2Strategy}-vs-{Ai3Strategy}"
            : $"{Ai1Strategy}-vs-{Ai2Strategy}";
    }
}
