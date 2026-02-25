using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface IHaggisScoringStrategy
    {
        int RunOutMultiplier { get; }
        int GameOverScore { get; }
        int GetCardPoints(Card card);
    }
}
