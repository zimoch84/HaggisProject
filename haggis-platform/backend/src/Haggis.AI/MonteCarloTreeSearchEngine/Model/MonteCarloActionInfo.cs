using Haggis.Domain.Model;

namespace Haggis.AI.Model
{
    public class MonteCarloActionInfo
    {
        public HaggisAction Action { get; set; }
        public int NumRuns { get; set; }
        public double NumWins { get; set; }
        public double WinRate => NumRuns == 0 ? 0 : (double)NumWins / NumRuns;
    }
}
