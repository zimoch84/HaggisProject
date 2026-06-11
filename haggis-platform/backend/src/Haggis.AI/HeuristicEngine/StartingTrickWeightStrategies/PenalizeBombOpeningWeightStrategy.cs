using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.StartingTrickWeightStrategies
{
    public sealed class PenalizeBombOpeningWeightStrategy : IStartingTrickWeightStrategy
    {
        private int BombOpeningPenaltyFactor { get; }

        public PenalizeBombOpeningWeightStrategy(int bombOpeningPenaltyFactor)
        {
            BombOpeningPenaltyFactor = bombOpeningPenaltyFactor;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            return (allSuggestedTricks ?? new List<Trick>())
                .Select(trick => (GetWeightForTrick(trick, gameState), trick))
                .ToList();
        }

        private int GetWeightForTrick(Trick trick, RoundState gameState)
        {
            if (trick == null || gameState?.CurrentPlayer?.Hand == null || BombOpeningPenaltyFactor <= 0)
            {
                return 0;
            }

            if (trick.Type != TrickType.BOMB)
            {
                return 0;
            }

            return -(gameState.CurrentPlayer.Hand.Count * BombOpeningPenaltyFactor);
        }
    }
}
