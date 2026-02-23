using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.Domain.Model;
using Haggis.Domain.Interfaces;

namespace Haggis.Domain.Extentions
{
    public static class HaggisActionExtensions
    {
        /// <summary>
        /// Returns true if the action represents a pass.
        /// </summary>
        public static bool IsPassAction(this HaggisAction action)
        {
            return action != null && action.IsPass;
        }

        /// <summary>
        /// Returns the GUID of the player associated with the action, or Guid.Empty if null.
        /// </summary>
        public static Guid PlayerGuid(this HaggisAction action)
        {
            if (action == null || action.Player == null)
                return Guid.Empty;
            return action.Player.GUID;
        }

        /// <summary>
        /// Apply scoring when a player runs out of cards. Returns true if scoring applied.
        /// </summary>
        public static bool ScoreForRunOut(this HaggisAction action, List<IHaggisPlayer> players, int multiplier)
        {
            if (action == null || players == null)
                return false;

            var target = players.FirstOrDefault(p => p.GUID == action.Player.GUID);
            if (target == null)
                return false;

            if (!target.Finished)
                return false;

            foreach (var p in players.Where(p => !p.Finished && p.GUID != action.Player.GUID))
            {
                target.Score += p.Hand.Count * multiplier;
            }

            return true;
        }
    }
}
