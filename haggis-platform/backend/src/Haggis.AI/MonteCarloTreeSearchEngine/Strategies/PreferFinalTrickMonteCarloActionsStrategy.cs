using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarlo
{
    public sealed class PreferFinalTrickMonteCarloActionsStrategy : IMonteCarloActionSelectionStrategy
    {
        public IList<HaggisAction> Select(RoundState state, IList<HaggisAction> generatedActions)
        {
            var finalActions = generatedActions
                .Where(action => !action.IsPass && action.Trick != null && action.Trick.Cards.Count == action.Player.Hand.Count)
                .ToList();

            if (finalActions.Any())
            {
                finalActions.ForEach(action => action.Trick.IsFinal = true);
                return finalActions;
            }

            return generatedActions.ToList();
        }
    }
}
