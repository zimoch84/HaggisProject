using Haggis.AI.Interfaces;
using Haggis.AI.Model;
using Haggis.AI.StartingTrickFilterStrategies;
using Haggis.Domain.Model;
using System.Linq;
using System.Collections.Generic;

namespace Haggis.AI.Strategies
{
    public sealed class StartingTrickStrategy : IPlayStrategy
    {
        private IStartingTrickFilterStrategy StartingTrickFilterStrategy { get; }

        public StartingTrickStrategy(IStartingTrickFilterStrategy startingTrickFilterStrategy = null)
        {
            StartingTrickFilterStrategy = startingTrickFilterStrategy ?? new FilterNoneStrategy();
        }

        public HaggisAction GetPlayingAction(RoundState gameState)
        {
            if (gameState.CurrentTrickPlay.LastAction != null)
            {
                return null;
            }

            var PossibleActions = gameState.PossibleActions.Where(a => !a.IsPass).ToList();
            if (!PossibleActions.Any())
            {
                return null;
            }

            var aiPlayer = gameState.CurrentPlayer as AIPlayer;
            if (aiPlayer == null)
            {
                return null;
            }

            var suggestedTricks = aiPlayer.SuggestedTricks(null);
            var filteredTricks = StartingTrickFilterStrategy.FilterTricks(new List<Trick>(suggestedTricks));
            foreach (var trick in filteredTricks)
            {
                var action = HaggisAction.FromTrick(trick, gameState.CurrentPlayer);
                if (PossibleActions.Contains(action))
                {
                    return action;
                }
            }

            return null;
        }
    }
}


