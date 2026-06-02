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
        private static HashSet<TrickType> SameCardTypes { get; } = new HashSet<TrickType> { SINGLE, PAIR, TRIPLE, QUAD, FIVED, SIXED };
        private static HashSet<TrickType> SequenceTypes { get; } = new HashSet<TrickType> { SEQ3, SEQ4, SEQ5, SEQ6, SEQ7 };
        private static HashSet<TrickType> PairedSequenceType { get; } = new HashSet<TrickType> { PAIRSEQ2, PAIRSEQ3, PAIRSEQ4, PAIRSEQ5, PAIRSEQ6, PAIRSEQ7 };

        protected List<Trick> BuildAllPossibleTricks(IHaggisPlayer player, TrickType? lastTrickType)
        {
            var tricks = new List<Trick>();
            var handIndex = HandIndex.Build(player?.Hand);

            if (!lastTrickType.HasValue)
            {
                foreach (var trickType in SameCardTypes)
                {
                    tricks.AddRange(handIndex.FindTheSameCards(trickType));
                    tricks.AddRange(handIndex.FindTheSameCardsWithWildCards(trickType));
                }

                foreach (var trickType in SequenceTypes)
                {
                    tricks.AddRange(handIndex.FindCardSequences(trickType));
                }

                foreach (var trickType in PairedSequenceType)
                {
                    tricks.AddRange(handIndex.FindPairedSequences(trickType));
                }
            }
            else
            {
                if (SameCardTypes.Contains(lastTrickType.Value))
                {
                    tricks.AddRange(handIndex.FindTheSameCards(lastTrickType.Value));
                    tricks.AddRange(handIndex.FindTheSameCardsWithWildCards(lastTrickType.Value));
                }

                if (SequenceTypes.Contains(lastTrickType.Value))
                {
                    tricks.AddRange(handIndex.FindCardSequences(lastTrickType.Value));
                }

                if (PairedSequenceType.Contains(lastTrickType.Value))
                {
                    tricks.AddRange(handIndex.FindPairedSequences(lastTrickType.Value));
                }
            }

            tricks.AddRange(handIndex.FindAllPossibleBombs());
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
