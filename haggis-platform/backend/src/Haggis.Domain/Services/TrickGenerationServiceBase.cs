using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using static Haggis.Domain.Enums.TrickType;

namespace Haggis.Domain.Services
{
    public abstract class TrickGenerationServiceBase
    {
        private readonly Action<TrickGenerationPhase, long> onTiming;
        private static HashSet<TrickType> SameCardTypes { get; } = new HashSet<TrickType> { SINGLE, PAIR, TRIPLE, QUAD, FIVED, SIXED };
        private static HashSet<TrickType> SequenceTypes { get; } = new HashSet<TrickType> { SEQ3, SEQ4, SEQ5, SEQ6, SEQ7 };
        private static HashSet<TrickType> StairTypes { get; } = new HashSet<TrickType>
        {
            PAIRSEQ2, PAIRSEQ3, PAIRSEQ4, PAIRSEQ5, PAIRSEQ6, PAIRSEQ7,
            TRIPLESTAIR2, TRIPLESTAIR3, TRIPLESTAIR4, TRIPLESTAIR5,
            QUADSTAIR2, QUADSTAIR3, QUADSTAIR4,
            FIVEDSTAIR2, FIVEDSTAIR3
        };

        protected TrickGenerationServiceBase(Action<TrickGenerationPhase, long> onTiming = null)
        {
            this.onTiming = onTiming;
        }

        protected List<Trick> BuildAllPossibleTricks(IHaggisPlayer player, TrickType? lastTrickType)
        {
            var tricks = new List<Trick>();
            var handIndexStart = Stopwatch.GetTimestamp();
            var handIndex = HandIndex.Build(player?.Hand);
            onTiming?.Invoke(TrickGenerationPhase.HandIndex, Stopwatch.GetTimestamp() - handIndexStart);

            if (!lastTrickType.HasValue)
            {
                foreach (var trickType in SameCardTypes)
                {
                    AddMeasured(tricks, () => handIndex.FindTheSameCards(trickType), TrickGenerationPhase.SameCards);
                    AddMeasured(tricks, () => handIndex.FindTheSameCardsWithWildCards(trickType), TrickGenerationPhase.SameCardsWithWilds);
                }

                AddMeasured(tricks, handIndex.FindAllCardSequences, TrickGenerationPhase.Sequences);

                AddMeasured(tricks, handIndex.FindAllStairs, TrickGenerationPhase.Stairs);
            }
            else
            {
                if (SameCardTypes.Contains(lastTrickType.Value))
                {
                    AddMeasured(tricks, () => handIndex.FindTheSameCards(lastTrickType.Value), TrickGenerationPhase.SameCards);
                    AddMeasured(tricks, () => handIndex.FindTheSameCardsWithWildCards(lastTrickType.Value), TrickGenerationPhase.SameCardsWithWilds);
                }

                if (SequenceTypes.Contains(lastTrickType.Value))
                {
                    AddMeasured(tricks, () => handIndex.FindCardSequences(lastTrickType.Value), TrickGenerationPhase.Sequences);
                }

                if (StairTypes.Contains(lastTrickType.Value))
                {
                    AddMeasured(tricks, () => handIndex.FindStairs(lastTrickType.Value), TrickGenerationPhase.Stairs);
                }
            }

            AddMeasured(tricks, handIndex.FindAllPossibleBombs, TrickGenerationPhase.Bombs);
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

            var filterStart = Stopwatch.GetTimestamp();
            var filteredTricks = allPossibleTricks
                .Where(trick => trick.CompareTo(lastTrick) > 0)
                .ToList();
            onTiming?.Invoke(TrickGenerationPhase.ContinuationFilter, Stopwatch.GetTimestamp() - filterStart);
            return filteredTricks;
        }

        private void AddMeasured(List<Trick> target, Func<List<Trick>> generator, TrickGenerationPhase phase)
        {
            var start = Stopwatch.GetTimestamp();
            target.AddRange(generator());
            onTiming?.Invoke(phase, Stopwatch.GetTimestamp() - start);
        }
    }
}
