using Haggis.AI.Interfaces;
using Haggis.Domain.Model;
using System;

namespace Haggis.AI.Strategies
{
    public sealed class RandomPlayStrategy : IPlayStrategy
    {
        private static readonly Random Random = new Random();
        private static readonly object RandomLock = new object();

        public HaggisAction GetPlayingAction(RoundState gameState)
        {
            if (gameState?.PossibleActions is null || gameState.PossibleActions.Count == 0)
            {
                throw new InvalidOperationException("No legal moves are available for random AI strategy.");
            }

            int randomIndex;
            lock (RandomLock)
            {
                randomIndex = Random.Next(0, gameState.PossibleActions.Count);
            }

            return gameState.PossibleActions[randomIndex];
        }
    }
}
