using System.Collections.Generic;
using Haggis.Domain.Enums;
using Haggis.Domain.Interfaces;

namespace Haggis.Domain.Model
{
    public sealed class ClassicHaggisScoringStrategy : IHaggisScoringStrategy
    {
        private static readonly IReadOnlyDictionary<Rank, int> CardPoints = new Dictionary<Rank, int>
        {
            [Rank.THREE] = 1,
            [Rank.FIVE] = 1,
            [Rank.SEVEN] = 1,
            [Rank.NINE] = 1,
            [Rank.JACK] = 2,
            [Rank.QUEEN] = 3,
            [Rank.KING] = 5
        };

        public int RunOutMultiplier { get; }

        public ClassicHaggisScoringStrategy(int runOutMultiplier = 5)
        {
            RunOutMultiplier = runOutMultiplier;
        }

        public int GetCardPoints(Card card)
        {
            if (card == null)
                return 0;

            return CardPoints.TryGetValue(card.Rank, out var points) ? points : 0;
        }
    }
}
