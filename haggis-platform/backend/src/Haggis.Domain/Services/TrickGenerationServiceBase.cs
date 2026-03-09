using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;
using static Haggis.Domain.Enums.TrickType;

namespace Haggis.Domain.Services
{
    public abstract class TrickGenerationServiceBase
    {
        private static List<TrickType> SameCardTypes { get; } = new List<TrickType> { SINGLE, PAIR, TRIPLE, QUAD, FIVED, SIXED };
        private static List<TrickType> SequenceTypes { get; } = new List<TrickType> { SEQ3, SEQ4, SEQ5, SEQ6, SEQ6, SEQ7 };
        private static List<TrickType> PairedSequenceType { get; } = new List<TrickType> { PAIRSEQ2 };

        protected List<Trick> BuildAllPossibleTricks(IHaggisPlayer player, TrickType? lastTrickType)
        {
            var tricks = new List<Trick>();

            if (!lastTrickType.HasValue)
            {
                foreach (var trickType in SameCardTypes)
                {
                    tricks.AddRange(player.Hand.FindTheSameCards(trickType));
                    tricks.AddRange(player.Hand.FindTheSameCardsWithWildCards(trickType));
                }

                foreach (var trickType in SequenceTypes)
                {
                    tricks.AddRange(player.Hand.FindCardSequences(trickType));
                }

                foreach (var trickType in PairedSequenceType)
                {
                    tricks.AddRange(player.Hand.FindPairedSequences(trickType));
                }
            }
            else
            {
                if (SameCardTypes.Contains(lastTrickType.Value))
                {
                    tricks.AddRange(player.Hand.FindTheSameCards(lastTrickType.Value));
                    tricks.AddRange(player.Hand.FindTheSameCardsWithWildCards(lastTrickType.Value));
                }

                if (SequenceTypes.Contains(lastTrickType.Value))
                {
                    tricks.AddRange(player.Hand.FindCardSequences(lastTrickType.Value));
                }

                if (PairedSequenceType.Contains(lastTrickType.Value))
                {
                    tricks.AddRange(player.Hand.FindPairedSequences(lastTrickType.Value));
                }
            }

            tricks.AddRange(player.Hand.FindAllPossibleBombs());
            return tricks;
        }

        protected List<Trick> BuildPossibleOpeningTricks(IHaggisPlayer player)
        {
            return BuildAllPossibleTricks(player, null);
        }

        protected List<Trick> BuildPossibleContinuationTricks(IHaggisPlayer player, Trick lastTrick)
        {
            var allPossibleTricks = BuildAllPossibleTricks(player, lastTrick?.Type);
            if (lastTrick == null)
            {
                return allPossibleTricks;
            }

            return allPossibleTricks
                .Where(trick => trick.CompareTo(lastTrick) > 0)
                .ToList();
        }
    }
}
