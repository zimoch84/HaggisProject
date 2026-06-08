using System.Linq;
using System.IO;
using Haggis.AI.Benchmark;
using MonteCarlo;
using NUnit.Framework;

namespace HaggisTests
{
    [TestFixture]
    public sealed class AiBenchmarkTests
    {
        [Test]
        public void Run_ShouldCompleteNormalVsNormalGame()
        {
            var options = new AiBenchmarkOptions
            {
                Games = 1,
                Players = 3,
                SeedStart = 1,
                GameOverScore = 40,
                Ai1Strategy = "normal",
                Ai2Strategy = "normal",
                Ai3Strategy = "normal",
                Rotate = false,
                MaxMovesPerGame = 2000
            };

            var result = new AiBenchmarkRunner().Run(options).Single();

            Assert.That(result.Completed, Is.True, result.Error);
            Assert.That(result.Moves, Is.GreaterThan(0));
            Assert.That(result.Rounds, Is.GreaterThan(0));
            Assert.That(result.GameElapsedMs, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Winner, Is.Not.Empty);
        }

        [Test]
        public void Run_WithRotation_ShouldCreateResultForEachSeat()
        {
            var options = new AiBenchmarkOptions
            {
                Games = 1,
                Players = 3,
                SeedStart = 2,
                GameOverScore = 40,
                Ai1Strategy = "normal",
                Ai2Strategy = "random",
                Ai3Strategy = "normal",
                Rotate = true,
                MaxMovesPerGame = 10000
            };

            var results = new AiBenchmarkRunner().Run(options);

            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(results.Select(result => result.Rotation), Is.EquivalentTo(new[] { 0, 1, 2 }));
            Assert.That(
                results.All(result => result.Completed),
                Is.True,
                string.Join("\n", results.Where(result => !result.Completed).Select(result => result.Error)));
        }

        [Test]
        public void Summary_ShouldCalculateWinRates()
        {
            var results = new[]
            {
                new AiBenchmarkGameResult { Completed = true, WinnerStrategy = "normal", WinnerSeat = 1 },
                new AiBenchmarkGameResult { Completed = true, WinnerStrategy = "random", WinnerSeat = 2 },
                new AiBenchmarkGameResult { Completed = true, WinnerStrategy = "normal", WinnerSeat = 1 },
                new AiBenchmarkGameResult { Completed = false, Error = "failed" }
            };

            var summary = new AiBenchmarkSummary(results);

            Assert.That(summary.TotalGames, Is.EqualTo(4));
            Assert.That(summary.CompletedGames, Is.EqualTo(3));
            Assert.That(summary.FailedGames, Is.EqualTo(1));
            Assert.That(summary.WinRateByStrategy["normal"], Is.EqualTo(66.666).Within(0.01));
            Assert.That(summary.WinRateByStrategy["random"], Is.EqualTo(33.333).Within(0.01));
            Assert.That(summary.WinRateBySeat[1], Is.EqualTo(66.666).Within(0.01));
        }

        [Test]
        public void StrategyFactory_ShouldSupportParameterizedMonteCarlo()
        {
            Assert.DoesNotThrow(() => AiBenchmarkStrategyFactory.EnsureSupported("montecarlo:800:100"));
            Assert.DoesNotThrow(() => AiBenchmarkStrategyFactory.EnsureSupported("montecarlo:800:100:4"));
            Assert.That(AiBenchmarkStrategyFactory.Create("montecarlo:800:100"), Is.TypeOf<Haggis.AI.Strategies.MonteCarloStrategy>());
            Assert.Throws<System.ArgumentException>(() => AiBenchmarkStrategyFactory.EnsureSupported("montecarlo:0:100"));
            Assert.Throws<System.ArgumentException>(() => AiBenchmarkStrategyFactory.EnsureSupported("montecarlo:800:0"));
            Assert.Throws<System.ArgumentException>(() => AiBenchmarkStrategyFactory.EnsureSupported("montecarlo:800:100:0"));
        }

        [Test]
        public void ArgumentParser_ShouldUseDefaultsAndOverrideValues()
        {
            var defaults = AiBenchmarkArgumentParser.Parse(new string[0]);
            var custom = AiBenchmarkArgumentParser.Parse(new[]
            {
                "--games=12",
                "--ai1=random",
                "--ai2=montecarlo:800:100:4",
                "--ai3=normal",
                "--log=games.log"
            });

            Assert.That(defaults.Games, Is.EqualTo(1000));
            Assert.That(defaults.Ai1Strategy, Is.EqualTo("normal"));
            Assert.That(defaults.Ai2Strategy, Is.EqualTo("normal"));
            Assert.That(defaults.Ai3Strategy, Is.EqualTo("normal"));
            Assert.That(custom.Games, Is.EqualTo(12));
            Assert.That(custom.Ai1Strategy, Is.EqualTo("random"));
            Assert.That(custom.Ai2Strategy, Is.EqualTo("montecarlo:800:100:4"));
            Assert.That(custom.Ai3Strategy, Is.EqualTo("normal"));
            Assert.That(custom.LogPath, Is.EqualTo("games.log"));
        }

        [Test]
        public void OutputPath_ShouldAppendTimestampAndStrategiesBeforeExtension()
        {
            var options = new AiBenchmarkOptions
            {
                Ai1Strategy = "normal",
                Ai2Strategy = "montecarlo:800:100:4",
                Ai3Strategy = "normal"
            };

            var path = AiBenchmarkOutputPath.WithRunSuffix(
                "reports/results.csv",
                options,
                new System.DateTime(2026, 5, 31, 14, 5, 6, 789));

            Assert.That(path.Replace('\\', '/'), Is.EqualTo("reports/results_20260531_140506_normal-vs-montecarlo-800-100-4-vs-normal.csv"));
        }

        [Test]
        public void Run_ShouldCaptureGameLogLines()
        {
            var options = new AiBenchmarkOptions
            {
                Games = 1,
                Players = 3,
                SeedStart = 1,
                GameOverScore = 40,
                Ai1Strategy = "normal",
                Ai2Strategy = "random",
                Ai3Strategy = "normal",
                Rotate = false,
                MaxMovesPerGame = 2000
            };

            var result = new AiBenchmarkRunner().Run(options).Single();

            Assert.That(result.LogLines, Has.Some.Contains("GAME seed=1 rotation=0"));
            Assert.That(result.LogLines, Has.Some.Contains("ROUND 1 START current=p1"));
            Assert.That(result.LogLines, Has.Some.Contains("hand p1: 2R 2B 2G"));
            Assert.That(result.LogLines, Has.Some.Contains("TRICK 1 START current=p1"));
            Assert.That(result.LogLines, Has.Some.Contains("move 1: player=p1"));
            Assert.That(result.LogLines, Has.Some.Contains("GAME END"));
        }

        [Test]
        public void Run_WithRotation_ShouldRotateInitialStartingPlayer()
        {
            var options = new AiBenchmarkOptions
            {
                Games = 1,
                Players = 3,
                SeedStart = 3,
                GameOverScore = 40,
                Ai1Strategy = "normal",
                Ai2Strategy = "random",
                Ai3Strategy = "normal",
                Rotate = true,
                MaxMovesPerGame = 10000
            };

            var results = new AiBenchmarkRunner().Run(options);

            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(
                results.Select(result =>
                    result.LogLines.Single(line => line.Contains("ROUND 1 START current=")).Trim()).ToArray(),
                Is.EqualTo(new[]
                {
                    "ROUND 1 START current=p1",
                    "ROUND 1 START current=p2",
                    "ROUND 1 START current=p3"
                }));
            Assert.That(
                results.Select(result => result.LogLines.First(line => line.Contains("move 1:"))).ToArray(),
                Is.Unique);
        }

        [Test]
        public void Run_WithSeatStrategies_ShouldKeepStrategiesOnConfiguredSeats()
        {
            var options = new AiBenchmarkOptions
            {
                Games = 1,
                Players = 3,
                SeedStart = 1,
                GameOverScore = 40,
                Ai1Strategy = "heuristic-continuations",
                Ai2Strategy = "random",
                Ai3Strategy = "normal",
                Rotate = true,
                MaxMovesPerGame = 10000
            };

            var results = new AiBenchmarkRunner().Run(options);

            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(results[0].StrategiesByPlayer["p1"], Is.EqualTo("heuristic-continuations"));
            Assert.That(results[0].StrategiesByPlayer["p2"], Is.EqualTo("random"));
            Assert.That(results[0].StrategiesByPlayer["p3"], Is.EqualTo("normal"));
            Assert.That(results[1].StrategiesByPlayer["p1"], Is.EqualTo("heuristic-continuations"));
            Assert.That(results[1].StrategiesByPlayer["p2"], Is.EqualTo("random"));
            Assert.That(results[1].StrategiesByPlayer["p3"], Is.EqualTo("normal"));
            Assert.That(results[2].StrategiesByPlayer["p1"], Is.EqualTo("heuristic-continuations"));
            Assert.That(results[2].StrategiesByPlayer["p2"], Is.EqualTo("random"));
            Assert.That(results[2].StrategiesByPlayer["p3"], Is.EqualTo("normal"));
        }

        [Test]
        public void Run_WithMonteCarloStrategy_ShouldLogMctsRootStats()
        {
            var options = new AiBenchmarkOptions
            {
                Games = 1,
                Players = 3,
                SeedStart = 1,
                GameOverScore = 40,
                Ai1Strategy = "normal",
                Ai2Strategy = "montecarlo:20:1",
                Ai3Strategy = "normal",
                Rotate = false,
                MaxMovesPerGame = 10000
            };

            var result = new AiBenchmarkRunner().Run(options).Single();
            var header = result.LogLines.FirstOrDefault(line => line.Contains("mcts:"));

            Assert.That(result.Completed, Is.True, result.Error);
            Assert.That(header, Is.Not.Null);
            Assert.That(header, Does.Contain("iterations="));
            Assert.That(header, Does.Contain("budgetMs="));
            Assert.That(header, Does.Contain("elapsedMs="));
            Assert.That(header, Does.Contain("workers="));
            Assert.That(header, Does.Contain("legalActions="));
            Assert.That(header, Does.Contain("rootChildren="));
            Assert.That(header, Does.Contain("treeNodes="));
            Assert.That(header, Does.Contain("treeDepth="));
            Assert.That(header, Does.Contain("scheduledRollouts="));
            Assert.That(header, Does.Contain("completedRollouts="));
            Assert.That(result.Timing, Is.Not.Null);
            Assert.That(result.LogLines.Any(line => line.Contains("timing:") && line.Contains("searchMs=") && line.Contains("cloneStateMs=") && line.Contains("moveGenerationMs=")), Is.True);
            Assert.That(
                result.LogLines.Any(line =>
                    line.Contains("1. action=") &&
                    line.Contains("runs=") &&
                    line.Contains("wins=") &&
                    line.Contains("winRate=")),
                Is.True);
        }

        [Test]
        public void Summary_ShouldIncludeAverageTimingWhenAvailable()
        {
            var results = new[]
            {
                new AiBenchmarkGameResult
                {
                    Completed = true,
                    Timing = new MonteCarlo.MctsTimingResult
                    {
                        CompletedRollouts = 5,
                        SearchMs = 10,
                        SchedulerMs = 2,
                        CloneStateMs = 1,
                        MoveGenerationMs = 3,
                        MoveGenerationTreeMs = 1.2,
                        MoveGenerationRolloutMs = 1.8,
                        MoveGenerationHandIndexMs = 0.4,
                        MoveGenerationSameCardsMs = 0.5,
                        MoveGenerationSameCardsWithWildsMs = 0.6,
                        MoveGenerationSequencesMs = 0.7,
                        MoveGenerationStairsMs = 0.8,
                        MoveGenerationBombsMs = 0.9,
                        MoveGenerationContinuationFilterMs = 1.1,
                        MoveGenerationTrickSelectionMs = 0.2,
                        MoveGenerationActionWrappingMs = 0.3,
                        MoveGenerationActionSelectionMs = 0.4,
                        MoveGenerationPassAppendMs = 0.1,
                        SelectionMs = 1,
                        ExpansionMs = 1,
                        RolloutMs = 1,
                        BackpropagationMs = 1
                    }
                }
            };

            var summary = new AiBenchmarkSummary(results);

            Assert.That(summary.Format(), Does.Contain("MCTS timing"));
            Assert.That(summary.Format(), Does.Contain("scheduler/task overhead"));
            Assert.That(summary.Format(), Does.Contain("move generation per iteration"));
            Assert.That(summary.Format(), Does.Contain("tree"));
            Assert.That(summary.Format(), Does.Contain("rollout"));
            Assert.That(summary.Format(), Does.Contain("same cards with wilds"));
            Assert.That(summary.Format(), Does.Contain("action wrapping"));
            Assert.That(summary.Format(), Does.Contain("Total simulated game time"));
        }

        [Test]
        public void TimingCollector_ShouldSplitTreeAndRolloutMoveGeneration()
        {
            var collector = new MctsTimingCollector();

            collector.AddMoveGenerationTree(StopwatchTicksFromMs(1.25));
            collector.AddMoveGenerationRollout(StopwatchTicksFromMs(2.75));

            var result = collector.Snapshot(0, 3, 2, 1);

            Assert.That(result.MoveGenerationTreeMs, Is.EqualTo(1.25).Within(0.05));
            Assert.That(result.MoveGenerationRolloutMs, Is.EqualTo(2.75).Within(0.05));
            Assert.That(result.MoveGenerationMs, Is.EqualTo(result.MoveGenerationTreeMs + result.MoveGenerationRolloutMs).Within(0.001));
        }

        [Test]
        public void CsvWriter_ShouldIncludeTreeAndRolloutMoveGenerationColumns()
        {
            var path = Path.GetTempFileName();
            try
            {
                AiBenchmarkCsvWriter.Write(path, new[]
                {
                    new AiBenchmarkGameResult
                    {
                        Completed = true,
                        Timing = new MctsTimingResult
                        {
                            CompletedRollouts = 1,
                            MoveGenerationMs = 3,
                            MoveGenerationTreeMs = 1,
                            MoveGenerationRolloutMs = 2
                        }
                    }
                });

                var header = File.ReadLines(path).First();
                Assert.That(header, Does.Contain("mctsMoveGenerationTreeMs"));
                Assert.That(header, Does.Contain("mctsMoveGenerationRolloutMs"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static long StopwatchTicksFromMs(double milliseconds)
        {
            return (long)(milliseconds * System.Diagnostics.Stopwatch.Frequency / 1000.0);
        }
    }
}
