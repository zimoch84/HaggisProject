using Haggis.Domain.Interfaces;
using Haggis.Domain.Enums;

namespace Haggis.Domain.Model
{
    public sealed class EveryCardOnePointScoringStrategy : IHaggisScoringStrategy
    {
        public int RunOutMultiplier { get; }
        public int GameOverScore { get; }

        public EveryCardOnePointScoringStrategy(int runOutMultiplier = 5, int gameOverScore = 250)
        {
            RunOutMultiplier = runOutMultiplier;
            GameOverScore = gameOverScore;
        }

        public int GetCardPoints(Card card)
        {
            if (card == null)
                return 0;

            if (!card.IsWild)
                return 1;

            switch (card.BaseRank)
            {
                case Rank.JACK:
                    return 2;
                case Rank.QUEEN:
                    return 3;
                case Rank.KING:
                    return 5;
                default:
                    return 1;
            }
        }
    }
}
