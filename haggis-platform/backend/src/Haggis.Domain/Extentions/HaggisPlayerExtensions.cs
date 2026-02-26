using System.Collections.Generic;
using System.Linq;
using Haggis.Domain.Model;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Services;

namespace Haggis.Domain.Extentions
{
    public static class IHaggisPlayerExtension
    {
        private static MoveGenerationService MoveGenerationService { get; } = new MoveGenerationService();

        public static IList<HaggisAction> GetPossibleActions(this IHaggisPlayer player, Trick lastTrick)
        {
            var actions = new List<HaggisAction>();
            var possibleTricks = MoveGenerationService.GetPossibleContinuationTricks(player, lastTrick);
            bool isFinalAction = possibleTricks.Any(t => t.Cards.Count == player.Hand.Count);

            possibleTricks.ForEach(trick => actions.Add(HaggisAction.FromTrick(trick, player)));

            // Play pass only when you can't finish current TrickPlay
            if (lastTrick != null && !isFinalAction)
                actions.Add(HaggisAction.Pass(player));

            return actions;
        }
    }
}
