using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.WeightNormalization;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;

namespace Haggis.AI.ContinuationTrickWeightStrategies
{
    public sealed class PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy : IContinuationTrickWeightStrategy
    {
        private const int DefaultEndgameHandThreshold = 7;
        private const int BaseWeight = 100;

        private float Weight { get; }

        public PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy(float weight)
        {
            Weight = weight;
        }

        public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
        {
            var tricks = allSuggestedTricks ?? new List<Trick>();
            if (gameState?.CurrentPlayer == null ||
                gameState.CurrentPlayer.Hand.Count > DefaultEndgameHandThreshold ||
                Weight <= 0)
            {
                return tricks.Select(trick => (0, trick)).ToList();
            }

            var passIsLegal = gameState.PossibleActions.Any(action => action.IsPass);
            if (!passIsLegal)
            {
                return tricks.Select(trick => (0, trick)).ToList();
            }

            return tricks
                .Select(trick => (trick?.Type == TrickType.BOMB
                    ? HeuristicWeightNormalization.ApplyWeight(BaseWeight, Weight)
                    : 0, trick))
                .ToList();
        }
    }
}
