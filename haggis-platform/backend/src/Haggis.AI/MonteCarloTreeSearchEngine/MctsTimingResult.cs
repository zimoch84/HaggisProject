using System;
using System.Diagnostics;
using System.Threading;

namespace MonteCarlo
{
    public sealed class MctsTimingResult
    {
        public int ScheduledRollouts { get; set; }
        public int CompletedRollouts { get; set; }
        public int Workers { get; set; }
        public double SearchMs { get; set; }
        public double SchedulerMs { get; set; }
        public double CloneStateMs { get; set; }
        public double MoveGenerationMs { get; set; }
        public double MoveGenerationTreeMs { get; set; }
        public double MoveGenerationRolloutMs { get; set; }
        public double MoveGenerationHandIndexMs { get; set; }
        public double MoveGenerationSameCardsMs { get; set; }
        public double MoveGenerationSameCardsWithWildsMs { get; set; }
        public double MoveGenerationSequencesMs { get; set; }
        public double MoveGenerationStairsMs { get; set; }
        public double MoveGenerationBombsMs { get; set; }
        public double MoveGenerationContinuationFilterMs { get; set; }
        public double MoveGenerationTrickSelectionMs { get; set; }
        public double MoveGenerationActionWrappingMs { get; set; }
        public double MoveGenerationActionSelectionMs { get; set; }
        public double MoveGenerationPassAppendMs { get; set; }
        public double SelectionMs { get; set; }
        public double ExpansionMs { get; set; }
        public double RolloutMs { get; set; }
        public double BackpropagationMs { get; set; }

        public double WorkerPhaseMs =>
            CloneStateMs +
            MoveGenerationMs +
            SelectionMs +
            ExpansionMs +
            RolloutMs +
            BackpropagationMs;

        public double SearchMsPerIteration => PerIteration(SearchMs);
        public double SchedulerMsPerIteration => PerIteration(SchedulerMs);
        public double CloneStateMsPerIteration => PerIteration(CloneStateMs);
        public double MoveGenerationMsPerIteration => PerIteration(MoveGenerationMs);
        public double MoveGenerationTreeMsPerIteration => PerIteration(MoveGenerationTreeMs);
        public double MoveGenerationRolloutMsPerIteration => PerIteration(MoveGenerationRolloutMs);
        public double MoveGenerationHandIndexMsPerIteration => PerIteration(MoveGenerationHandIndexMs);
        public double MoveGenerationSameCardsMsPerIteration => PerIteration(MoveGenerationSameCardsMs);
        public double MoveGenerationSameCardsWithWildsMsPerIteration => PerIteration(MoveGenerationSameCardsWithWildsMs);
        public double MoveGenerationSequencesMsPerIteration => PerIteration(MoveGenerationSequencesMs);
        public double MoveGenerationStairsMsPerIteration => PerIteration(MoveGenerationStairsMs);
        public double MoveGenerationBombsMsPerIteration => PerIteration(MoveGenerationBombsMs);
        public double MoveGenerationContinuationFilterMsPerIteration => PerIteration(MoveGenerationContinuationFilterMs);
        public double MoveGenerationTrickSelectionMsPerIteration => PerIteration(MoveGenerationTrickSelectionMs);
        public double MoveGenerationActionWrappingMsPerIteration => PerIteration(MoveGenerationActionWrappingMs);
        public double MoveGenerationActionSelectionMsPerIteration => PerIteration(MoveGenerationActionSelectionMs);
        public double MoveGenerationPassAppendMsPerIteration => PerIteration(MoveGenerationPassAppendMs);
        public double SelectionMsPerIteration => PerIteration(SelectionMs);
        public double ExpansionMsPerIteration => PerIteration(ExpansionMs);
        public double RolloutMsPerIteration => PerIteration(RolloutMs);
        public double BackpropagationMsPerIteration => PerIteration(BackpropagationMs);

        private double PerIteration(double totalMs)
        {
            return CompletedRollouts > 0 ? totalMs / CompletedRollouts : 0;
        }
    }

    public sealed class MctsTimingCollector
    {
        private long schedulerTicks;
        private long cloneStateTicks;
        private long moveGenerationTreeTicks;
        private long moveGenerationRolloutTicks;
        private long moveGenerationHandIndexTicks;
        private long moveGenerationSameCardsTicks;
        private long moveGenerationSameCardsWithWildsTicks;
        private long moveGenerationSequencesTicks;
        private long moveGenerationStairsTicks;
        private long moveGenerationBombsTicks;
        private long moveGenerationContinuationFilterTicks;
        private long moveGenerationTrickSelectionTicks;
        private long moveGenerationActionWrappingTicks;
        private long moveGenerationActionSelectionTicks;
        private long moveGenerationPassAppendTicks;
        private long selectionTicks;
        private long expansionTicks;
        private long rolloutTicks;
        private long backpropagationTicks;

        public void AddScheduler(long ticks)
        {
            Interlocked.Add(ref schedulerTicks, ticks);
        }

        public void AddCloneState(long ticks)
        {
            Interlocked.Add(ref cloneStateTicks, ticks);
        }

        public void AddMoveGenerationTree(long ticks)
        {
            Interlocked.Add(ref moveGenerationTreeTicks, ticks);
        }

        public void AddMoveGenerationRollout(long ticks)
        {
            Interlocked.Add(ref moveGenerationRolloutTicks, ticks);
        }

        public void AddMoveGenerationHandIndex(long ticks)
        {
            Interlocked.Add(ref moveGenerationHandIndexTicks, ticks);
        }

        public void AddMoveGenerationSameCards(long ticks)
        {
            Interlocked.Add(ref moveGenerationSameCardsTicks, ticks);
        }

        public void AddMoveGenerationSameCardsWithWilds(long ticks)
        {
            Interlocked.Add(ref moveGenerationSameCardsWithWildsTicks, ticks);
        }

        public void AddMoveGenerationSequences(long ticks)
        {
            Interlocked.Add(ref moveGenerationSequencesTicks, ticks);
        }

        public void AddMoveGenerationStairs(long ticks)
        {
            Interlocked.Add(ref moveGenerationStairsTicks, ticks);
        }

        public void AddMoveGenerationBombs(long ticks)
        {
            Interlocked.Add(ref moveGenerationBombsTicks, ticks);
        }

        public void AddMoveGenerationContinuationFilter(long ticks)
        {
            Interlocked.Add(ref moveGenerationContinuationFilterTicks, ticks);
        }

        public void AddMoveGenerationTrickSelection(long ticks)
        {
            Interlocked.Add(ref moveGenerationTrickSelectionTicks, ticks);
        }

        public void AddMoveGenerationActionWrapping(long ticks)
        {
            Interlocked.Add(ref moveGenerationActionWrappingTicks, ticks);
        }

        public void AddMoveGenerationActionSelection(long ticks)
        {
            Interlocked.Add(ref moveGenerationActionSelectionTicks, ticks);
        }

        public void AddMoveGenerationPassAppend(long ticks)
        {
            Interlocked.Add(ref moveGenerationPassAppendTicks, ticks);
        }

        public void AddSelection(long ticks)
        {
            Interlocked.Add(ref selectionTicks, ticks);
        }

        public void AddExpansion(long ticks)
        {
            Interlocked.Add(ref expansionTicks, ticks);
        }

        public void AddRollout(long ticks)
        {
            Interlocked.Add(ref rolloutTicks, ticks);
        }

        public void AddBackpropagation(long ticks)
        {
            Interlocked.Add(ref backpropagationTicks, ticks);
        }

        public MctsTimingResult Snapshot(long searchTicks, int scheduledRollouts, int completedRollouts, int workers)
        {
            var moveGenerationTreeMs = ToMilliseconds(Interlocked.Read(ref moveGenerationTreeTicks));
            var moveGenerationRolloutMs = ToMilliseconds(Interlocked.Read(ref moveGenerationRolloutTicks));
            return new MctsTimingResult
            {
                ScheduledRollouts = scheduledRollouts,
                CompletedRollouts = completedRollouts,
                Workers = workers,
                SearchMs = ToMilliseconds(searchTicks),
                SchedulerMs = ToMilliseconds(Interlocked.Read(ref schedulerTicks)),
                CloneStateMs = ToMilliseconds(Interlocked.Read(ref cloneStateTicks)),
                MoveGenerationMs = moveGenerationTreeMs + moveGenerationRolloutMs,
                MoveGenerationTreeMs = moveGenerationTreeMs,
                MoveGenerationRolloutMs = moveGenerationRolloutMs,
                MoveGenerationHandIndexMs = ToMilliseconds(Interlocked.Read(ref moveGenerationHandIndexTicks)),
                MoveGenerationSameCardsMs = ToMilliseconds(Interlocked.Read(ref moveGenerationSameCardsTicks)),
                MoveGenerationSameCardsWithWildsMs = ToMilliseconds(Interlocked.Read(ref moveGenerationSameCardsWithWildsTicks)),
                MoveGenerationSequencesMs = ToMilliseconds(Interlocked.Read(ref moveGenerationSequencesTicks)),
                MoveGenerationStairsMs = ToMilliseconds(Interlocked.Read(ref moveGenerationStairsTicks)),
                MoveGenerationBombsMs = ToMilliseconds(Interlocked.Read(ref moveGenerationBombsTicks)),
                MoveGenerationContinuationFilterMs = ToMilliseconds(Interlocked.Read(ref moveGenerationContinuationFilterTicks)),
                MoveGenerationTrickSelectionMs = ToMilliseconds(Interlocked.Read(ref moveGenerationTrickSelectionTicks)),
                MoveGenerationActionWrappingMs = ToMilliseconds(Interlocked.Read(ref moveGenerationActionWrappingTicks)),
                MoveGenerationActionSelectionMs = ToMilliseconds(Interlocked.Read(ref moveGenerationActionSelectionTicks)),
                MoveGenerationPassAppendMs = ToMilliseconds(Interlocked.Read(ref moveGenerationPassAppendTicks)),
                SelectionMs = ToMilliseconds(Interlocked.Read(ref selectionTicks)),
                ExpansionMs = ToMilliseconds(Interlocked.Read(ref expansionTicks)),
                RolloutMs = ToMilliseconds(Interlocked.Read(ref rolloutTicks)),
                BackpropagationMs = ToMilliseconds(Interlocked.Read(ref backpropagationTicks))
            };
        }

        private static double ToMilliseconds(long ticks)
        {
            return ticks * 1000.0 / Stopwatch.Frequency;
        }
    }
}
