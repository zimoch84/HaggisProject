using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;

namespace MonteCarlo
{
    public sealed class PreferFinalTrickMonteCarloActionsStrategy : IMonteCarloActionSelectionStrategy
    {
        public IList<MonteCarloHaggisAction> Select(RoundState state, IList<MonteCarloHaggisAction> generatedActions)
        {
            List<MonteCarloHaggisAction> finalActions = null;

            for (var index = 0; index < generatedActions.Count; index++)
            {
                var action = generatedActions[index];
                if (action.IsPass || action.Trick == null || action.Trick.Cards.Count != action.Player.Hand.Count)
                {
                    continue;
                }

                if (finalActions == null)
                {
                    finalActions = new List<MonteCarloHaggisAction>();
                }

                finalActions.Add(action.AsFinal());
            }

            return finalActions ?? generatedActions;
        }
    }
}
