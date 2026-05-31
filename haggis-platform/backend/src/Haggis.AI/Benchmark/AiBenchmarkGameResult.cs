using System.Collections.Generic;

namespace Haggis.AI.Benchmark
{
    public sealed class AiBenchmarkGameResult
    {
        public int Seed { get; set; }
        public int Rotation { get; set; }
        public bool Completed { get; set; }
        public string Error { get; set; }
        public string Winner { get; set; }
        public string WinnerStrategy { get; set; }
        public int WinnerSeat { get; set; }
        public int Rounds { get; set; }
        public int Moves { get; set; }
        public List<string> LogLines { get; set; } = new List<string>();
        public Dictionary<string, int> Scores { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> StrategiesByPlayer { get; set; } = new Dictionary<string, string>();
    }
}
