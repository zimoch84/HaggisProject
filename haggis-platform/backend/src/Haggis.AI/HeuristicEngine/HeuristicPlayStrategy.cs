using Haggis.AI.Interfaces;
using Haggis.AI.StartingTrickFilterStrategies;
using Haggis.Domain.Model;
using System.Collections.Generic;

namespace Haggis.AI.Strategies
{
    public sealed class HeuristicPlayStrategy : IPlayStrategy
    {
        private IList<IPlayStrategy> TrickStrategies { get; }

        public HeuristicPlayStrategy(
            IStartingTrickFilterStrategy startingTrickFilterStrategy = null,
            ContinuationTrickStrategy continuationTrickStrategy = null)
            : this(
                new StartingTrickStrategy(startingTrickFilterStrategy ?? new FilterNoneStrategy()),
                continuationTrickStrategy ?? new ContinuationTrickStrategy(false, true))
        {
        }

        public HeuristicPlayStrategy(
            StartingTrickStrategy startingTrickStrategy,
            ContinuationTrickStrategy continuationTrickStrategy)
        {
            TrickStrategies = new List<IPlayStrategy>
            {
                startingTrickStrategy,
                continuationTrickStrategy
            };
        }

        public HaggisAction GetPlayingAction(RoundState gameState)
        {
            foreach (var strategy in TrickStrategies)
            {
                var action = strategy.GetPlayingAction(gameState);
                if (action != null)
                {
                    return action;
                }
            }

            return gameState.PossibleActions[0];
        }
    }
}


