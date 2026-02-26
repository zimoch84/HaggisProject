using Haggis.AI.Interfaces;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace Haggis.AI.Strategies
{
    public sealed class ContinuationTrickStrategy : IPlayStrategy
    {
        private bool UseWildsInContinuations { get; }
        private bool TakeLessValueTrickFirst { get; }

        public ContinuationTrickStrategy(bool useWildsInContinuations, bool takeLessValueTrickFirst)
        {
            UseWildsInContinuations = useWildsInContinuations;
            TakeLessValueTrickFirst = takeLessValueTrickFirst;
        }

        public HaggisAction GetPlayingAction(HaggisGameState gameState)
        {
            var actions = gameState.PossibleActions;
            var tricks = actions.Where(a => !a.IsPass)
                .Select(a => a.Trick)
                .Where(trick => !trick.Type.Equals(TrickType.SINGLE))
                .Where(trick => trick.Cards.Where(c => c.IsWild).Count() == 0)
                .ToList();

            IEnumerable<Trick> filteredTricks;
            IEnumerable<Trick> groupedTricks;

            if (UseWildsInContinuations)
            {
                filteredTricks = tricks.Where(trick => tricks.HasContinuationWithWilds(trick));
            }
            else
            {
                filteredTricks = tricks.Where(trick => tricks.HasContinuation(trick));
            }

            if (TakeLessValueTrickFirst)
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

            if (!groupedTricks.Any())
            {
                return null;
            }

            var selectedTrick = groupedTricks.Last();
            return HaggisAction.FromTrick(selectedTrick, gameState.CurrentPlayer);
        }
    }
}


