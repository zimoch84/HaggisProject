using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using static Haggis.Domain.Extentions.CardsExtensions;
using static Haggis.Domain.Model.HaggisAction;

namespace HaggisTests
{
    [TestFixture]
    [Category("Perf")]
    [Explicit("Performance harness for move generation research.")]
    [NonParallelizable]
    internal class MoveGenerationPerformanceTests
    {
        private static readonly string ResultsPath = Path.Combine(
            AppContext.BaseDirectory,
            "move-generation-perf-results.csv");

        private sealed class TestableTrickGenerationService : TrickGenerationServiceBase
        {
            public List<Trick> Opening(IHaggisPlayer player) => BuildPossibleOpeningTricks(player);
            public List<Trick> Continuation(IHaggisPlayer player, Trick lastTrick) => BuildPossibleContinuationTricks(player, lastTrick);
        }

        private static readonly string[] Seed1Hand =
        {
            "9B", "5G", "8O", "2Y", "9Y", "8R", "6B", "10B", "9R", "9O", "8B", "2R", "2G", "7B", "J", "Q", "K"
        };

        private static readonly string[] Seed2Hand =
        {
            "3G", "8O", "2G", "3Y", "6O", "8G", "6B", "7R", "2Y", "7O", "5R", "7Y", "9B", "7G", "J", "Q", "K"
        };

        private static readonly string[] Seed3Hand =
        {
            "7Y", "5O", "9O", "9B", "9Y", "3G", "8O", "8B", "4Y", "7B", "8G", "6Y", "9G", "3Y", "J", "Q", "K"
        };

        private readonly TestableTrickGenerationService _trickGenerationService = new TestableTrickGenerationService();
        private readonly MoveGenerationService _moveGenerationService = new MoveGenerationService();

        [Test]
        public void Opening_Seed1FullHand_Perf()
        {
            var player = new HaggisPlayer("seed-1") { Hand = Seed1Hand.ToCards() };
            MeasureScenario(
                "opening seed1 full hand",
                () => _trickGenerationService.Opening(player).Count,
                warmupIterations: 3,
                measuredIterations: 300);
        }

        [Test]
        public void Opening_Seed2FullHand_Perf()
        {
            var player = new HaggisPlayer("seed-2") { Hand = Seed2Hand.ToCards() };
            MeasureScenario(
                "opening seed2 full hand",
                () => _trickGenerationService.Opening(player).Count,
                warmupIterations: 3,
                measuredIterations: 300);
        }

        [Test]
        public void Opening_Seed3FullHand_Perf()
        {
            var player = new HaggisPlayer("seed-3") { Hand = Seed3Hand.ToCards() };
            MeasureScenario(
                "opening seed3 full hand",
                () => _trickGenerationService.Opening(player).Count,
                warmupIterations: 3,
                measuredIterations: 300);
        }

        [Test]
        public void Sequence_WithWilds_Perf()
        {
            var hand = Cards("10R", "Q", "K");
            MeasureScenario(
                "sequence seq3 with wilds",
                () => hand.FindCardSequences(TrickType.SEQ3).Count,
                warmupIterations: 5,
                measuredIterations: 2000);
        }

        [Test]
        public void Stair_TripleStair3_Perf()
        {
            var hand = Cards("3R", "3B", "3G", "4R", "4B", "4G", "5R", "5B", "5G");
            MeasureScenario(
                "stairs triple stair3",
                () => hand.FindStairs(TrickType.TRIPLESTAIR3).Count,
                warmupIterations: 5,
                measuredIterations: 1500);
        }

        [Test]
        public void PairSequence_WithWilds_Perf()
        {
            var hand = Cards("10G", "10B", "J", "Q");
            MeasureScenario(
                "pair sequence2 with wilds",
                () => hand.FindPairedSequences(TrickType.PAIRSEQ2).Count,
                warmupIterations: 5,
                measuredIterations: 2000);
        }

        [Test]
        public void PossibleActions_OpeningState_Perf()
        {
            var state = CreateOpeningState();
            MeasureScenario(
                "possible actions opening state",
                () => _moveGenerationService.GetPossibleActionsForCurrentPlayer(state).Count,
                warmupIterations: 3,
                measuredIterations: 500);
        }

        [Test]
        public void PossibleActions_ContinuationState_Perf()
        {
            var state = CreateContinuationState();
            MeasureScenario(
                "possible actions continuation state",
                () => _moveGenerationService.GetPossibleActionsForCurrentPlayer(state).Count,
                warmupIterations: 3,
                measuredIterations: 500);
        }

        private static RoundState CreateOpeningState()
        {
            var p1 = new HaggisPlayer("p1") { Hand = Seed1Hand.ToCards() };
            var p2 = new HaggisPlayer("p2") { Hand = Cards("3Y", "4Y", "5Y", "6Y", "7Y") };
            var p3 = new HaggisPlayer("p3") { Hand = Cards("2B", "4B", "6B", "8B", "10B") };
            return new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
        }

        private static RoundState CreateContinuationState()
        {
            var p1 = new HaggisPlayer("p1") { Hand = Cards("10G", "10B", "J", "Q", "K", "9G") };
            var p2 = new HaggisPlayer("p2") { Hand = Cards("3Y", "4Y", "5Y", "6Y", "7Y") };
            var p3 = new HaggisPlayer("p3") { Hand = Cards("2B", "4B", "6B", "8B", "10B") };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            state.ApplyAction(FromTrick("9R_SINGLE", p1));
            return state;
        }

        private static void MeasureScenario(
            string scenario,
            Func<int> operation,
            int warmupIterations,
            int measuredIterations)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            var expectedCount = operation();
            Assert.That(expectedCount, Is.GreaterThan(0), $"{scenario} should produce results.");

            for (var warmup = 0; warmup < warmupIterations; warmup++)
            {
                Assert.That(operation(), Is.EqualTo(expectedCount), $"{scenario} warmup changed result count.");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();

            for (var iteration = 0; iteration < measuredIterations; iteration++)
            {
                var actualCount = operation();
                if (actualCount != expectedCount)
                {
                    Assert.Fail($"{scenario} changed result count during measurement. Expected {expectedCount}, got {actualCount}.");
                }
            }

            stopwatch.Stop();
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            var totalMs = stopwatch.Elapsed.TotalMilliseconds;
            var avgUsPerOperation = totalMs * 1000.0 / measuredIterations;
            var avgBytesPerOperation = allocatedBytes / (double)measuredIterations;

            TestContext.WriteLine(
                $"PERF scenario=\"{scenario}\" iterations={measuredIterations} resultCount={expectedCount} totalMs={totalMs:0.000} avgUsPerOp={avgUsPerOperation:0.000} avgBytesPerOp={avgBytesPerOperation:0.0}");

            AppendResult(scenario, measuredIterations, expectedCount, totalMs, avgUsPerOperation, avgBytesPerOperation);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            File.WriteAllText(
                ResultsPath,
                "scenario,iterations,resultCount,totalMs,avgUsPerOp,avgBytesPerOp" + Environment.NewLine);
        }

        private static void AppendResult(
            string scenario,
            int measuredIterations,
            int expectedCount,
            double totalMs,
            double avgUsPerOperation,
            double avgBytesPerOperation)
        {
            var line = string.Join(",",
                EscapeCsv(scenario),
                measuredIterations.ToString(CultureInfo.InvariantCulture),
                expectedCount.ToString(CultureInfo.InvariantCulture),
                totalMs.ToString("0.000", CultureInfo.InvariantCulture),
                avgUsPerOperation.ToString("0.000", CultureInfo.InvariantCulture),
                avgBytesPerOperation.ToString("0.0", CultureInfo.InvariantCulture));
            File.AppendAllText(ResultsPath, line + Environment.NewLine);
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Contains(",")
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
