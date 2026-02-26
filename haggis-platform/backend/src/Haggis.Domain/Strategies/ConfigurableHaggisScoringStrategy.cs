using System.Collections.Generic;
using Haggis.Domain.Enums;
using Haggis.Domain.Interfaces;

namespace Haggis.Domain.Model
{
    public sealed class ConfigurableHaggisScoringStrategy : IHaggisScoringStrategy
    {
        private readonly IReadOnlyDictionary<Rank, int> _cardPointsByRank;

        public int RunOutMultiplier { get; }

        public ConfigurableHaggisScoringStrategy(
            IReadOnlyDictionary<Rank, int> cardPointsByRank,
            int runOutMultiplier)
        {
            _cardPointsByRank = cardPointsByRank ?? new Dictionary<Rank, int>();
            RunOutMultiplier = runOutMultiplier;
        }

        public int GetCardPoints(Card card)
        {
            if (card == null)
                return 0;

            return _cardPointsByRank.TryGetValue(card.Rank, out var points) ? points : 0;
        }
    }
}
