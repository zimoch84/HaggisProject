using System.Collections.Generic;

namespace Haggis.Domain.Model
{
    public sealed class PlayerRoundScore
    {
        public string PlayerName { get; set; }
        public int RoundPoints { get; set; }
    }

    public sealed class RoundScoringResult
    {
        public int RoundNumber { get; set; }
        public string WinnerPlayerName { get; set; }
        public IReadOnlyList<PlayerRoundScore> PlayerScores { get; set; }
        public IReadOnlyList<string> FinishingOrderPlayerNames { get; set; }
    }
}
