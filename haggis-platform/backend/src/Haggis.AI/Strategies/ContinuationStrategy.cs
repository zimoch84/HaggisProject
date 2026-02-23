using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.AI.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.AI.Strategies
{
    /*
     * Has contiunuation means that you play non sinlge trick that you have also in  higher rank
     * takeLessValueTrickFirst means that we start from the least valued one
     * */

    public class ContinuationStrategy : IPlayStrategy
    {
        private bool _useWildsInContinuations = false;
        private bool _takeLessValueTrickFirst= true;

        public ContinuationStrategy(bool useWildsInContinuations, bool takeLessValueTrickFirst)
        {
            _useWildsInContinuations = useWildsInContinuations;
            _takeLessValueTrickFirst = takeLessValueTrickFirst;
        }

        public HaggisAction GetPlayingAction(HaggisGameState gameState)
        {
            var actions = gameState.Actions;
            var tricks = actions.Where(a => !a.IsPass)
                .ToList()
                .Select(a => a.Trick)
                .Where(trick => !trick.Type.Equals(TrickType.SINGLE))
                .Where(trick => trick.Cards.Where(c => c.IsWild).Count() == 0)
                .ToList();
            /*
             * TODO: tricks can be filtered to 0 
             */
            IEnumerable<Trick> filteredTricks;
            IEnumerable<Trick> groupedTricks;

            if (_useWildsInContinuations)
            {
                filteredTricks = tricks.Where(trick => tricks.HasContinuationWithWilds(trick));
            }
            else
            {
                filteredTricks = tricks.Where(trick => tricks.HasContinuation(trick));
            }

            if (_takeLessValueTrickFirst)
            {
                groupedTricks = filteredTricks
                    .GroupBy(trick => trick.Type)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.First());

            }
            else
            {
                groupedTricks = filteredTricks
                    .GroupBy(trick => trick.Type)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Last());
            }

            if(groupedTricks.Count() > 0)
            {
                var mostValubleTrick = groupedTricks.Last();
                var action = HaggisAction.FromTrick(mostValubleTrick, gameState.CurrentPlayer);
                return action;
            }

            return actions[0];
        }
    }
}
