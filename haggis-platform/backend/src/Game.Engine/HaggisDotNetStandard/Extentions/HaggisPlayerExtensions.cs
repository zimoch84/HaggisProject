using System.Collections.Generic;
using System.Linq;
using Haggis.Model;
using Haggis.Interfaces;

namespace Haggis.Extentions
{
    public static class IHaggisPlayerExtension
    {
        public static IList<HaggisAction> GetPossibleActions(this IHaggisPlayer player, Trick lastTrick)
        {
            var actions = new List<HaggisAction>();
            var possibleTricks = player.SuggestedTricks(lastTrick);

            bool isFinalAction = possibleTricks.Any(t => t.IsFinal);

            possibleTricks.ForEach(trick => actions.Add(HaggisAction.FromTrick(trick, player)));

            // Play pass only when you can't finish current TrickPlay
            if (lastTrick != null && !isFinalAction)
                actions.Add(HaggisAction.Pass(player));

            return actions;
        }
    }
}