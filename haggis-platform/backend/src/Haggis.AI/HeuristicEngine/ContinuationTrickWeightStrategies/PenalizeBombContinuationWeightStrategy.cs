using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PenalizeBombContinuationWeightStrategy : IContinuationTrickWeightStrategy
    {
        private int BombContinuationPenaltyFactor { get; }

        public PenalizeBombContinuationWeightStrategy(int bombContinuationPenaltyFactor)
        {
            BombContinuationPenaltyFactor = bombContinuationPenaltyFactor;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (trick?.Type == TrickType.BOMB ? -BombContinuationPenaltyFactor : 0, trick))
                .ToList();
        }
    }
}
