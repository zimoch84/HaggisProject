using Haggis.AI.Interfaces;
using Haggis.Domain.Model;
using System;
using System.Linq;

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

            var availableActions = gameState.CurrentTrickPlay.IsEmpty
                ? gameState.PossibleActions.Where(action => !action.IsPass).ToList()
                : gameState.PossibleActions.ToList();

            if (availableActions.Count == 0)
            {
                throw new InvalidOperationException("No playable non-pass moves are available for random AI strategy.");
            }

            int randomIndex;
            lock (RandomLock)
            {
                randomIndex = Random.Next(0, availableActions.Count);
            }

            return availableActions[randomIndex];
        }
    }
}
