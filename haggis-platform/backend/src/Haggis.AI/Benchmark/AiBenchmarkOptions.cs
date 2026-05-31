namespace Haggis.AI.Benchmark
{
    public sealed class AiBenchmarkOptions
    {
        public int Games { get; set; } = 1000;
        public int Players { get; set; } = 3;
        public int SeedStart { get; set; } = 1;
        public int GameOverScore { get; set; } = 250;
        public string Strategy { get; set; } = "normal";
        public string Opponent { get; set; } = "normal";
        public bool Rotate { get; set; } = true;
        public string CsvPath { get; set; }
        public string LogPath { get; set; }
        public int MaxMovesPerGame { get; set; } = 10000;
    }
}
