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
        private long moveGenerationTicks;
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

        public void AddMoveGeneration(long ticks)
        {
            Interlocked.Add(ref moveGenerationTicks, ticks);
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
            return new MctsTimingResult
            {
                ScheduledRollouts = scheduledRollouts,
                CompletedRollouts = completedRollouts,
                Workers = workers,
                SearchMs = ToMilliseconds(searchTicks),
                SchedulerMs = ToMilliseconds(Interlocked.Read(ref schedulerTicks)),
                CloneStateMs = ToMilliseconds(Interlocked.Read(ref cloneStateTicks)),
                MoveGenerationMs = ToMilliseconds(Interlocked.Read(ref moveGenerationTicks)),
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
