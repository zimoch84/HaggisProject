using System;
using Haggis.Domain.Model;

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
    }
}
