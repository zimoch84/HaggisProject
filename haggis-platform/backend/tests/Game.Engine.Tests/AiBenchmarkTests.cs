using System;
using System.Linq;
using System.IO;
using Haggis.AI.Benchmark;
using Haggis.AI.Strategies;
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
            Assert.That(summary.WinsByStrategy["normal"], Is.EqualTo(2));
            Assert.That(summary.WinRateByStrategy["normal"], Is.EqualTo(66.666).Within(0.01));
            Assert.That(summary.WinRateByStrategy["random"], Is.EqualTo(33.333).Within(0.01));
            Assert.That(summary.WinRateBySeat[1], Is.EqualTo(66.666).Within(0.01));
        }

        [Test]
        public void StrategyFactory_ShouldSupportParameterizedMonteCarlo()
        {
            Assert.DoesNotThrow(() => AiBenchmarkStrategyFactory.EnsureSupported("montecarlo:800:100"));
            Assert.DoesNotThrow(() => AiBenchmarkStrategyFactory.EnsureSupported("montecarlo:800:100:4"));
            Assert.DoesNotThrow(() => AiBenchmarkStrategyFactory.EnsureSupported("montecarlo:800:100:4:5:3"));
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
                "--ai1-weights=ContinuationFollowUpWeight=0.5",
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
            Assert.That(custom.Ai1HeuristicOptions.ContinuationFollowUpWeight, Is.EqualTo(0.5f));
            Assert.That(custom.Ai2Strategy, Is.EqualTo("montecarlo:800:100:4"));
            Assert.That(custom.Ai3Strategy, Is.EqualTo("normal"));
            Assert.That(custom.LogPath, Is.EqualTo("games.log"));
        }

        [Test]
        public void HeuristicTuningArgumentParser_ShouldParseMultipleWeightSets()
        {
            var options = HeuristicTuningArgumentParser.Parse(new[]
            {
                "--mode=tuning",
                "--games=100",
                "--seed-start=5",
                "--game-over-score=250",
                "--ai2=montecarlo:2000:2000:4",
                "--rotate=true",
                "--csv=results_heuristic-vs-montecarlo-2000-2000-4_100-seeds.csv",
                "--max-moves=20000",
                "--weights=ContinuationFollowUpWeight=1;BombOpeningWeight=1",
                "--weights=ContinuationFollowUpWeight=0.5;BombOpeningWeight=2"
            });

            Assert.That(options.Games, Is.EqualTo(100));
            Assert.That(options.SeedStart, Is.EqualTo(5));
            Assert.That(options.MonteCarloStrategy, Is.EqualTo("montecarlo:2000:2000:4"));
            Assert.That(options.CsvPath, Is.EqualTo("results_heuristic-vs-montecarlo-2000-2000-4_100-seeds.csv"));
            Assert.That(options.WeightSets, Has.Count.EqualTo(2));
            Assert.That(options.WeightSets[0].ContinuationFollowUpWeight, Is.EqualTo(1f));
            Assert.That(options.WeightSets[1].ContinuationFollowUpWeight, Is.EqualTo(0.5f));
            Assert.That(options.WeightSets[1].BombOpeningWeight, Is.EqualTo(2f));
        }

        [Test]
        public void HeuristicTuningArgumentParser_WithoutWeights_ShouldAllowAutoMode()
        {
            var options = HeuristicTuningArgumentParser.Parse(new[]
            {
                "--mode=tuning",
                "--games=100",
                "--seed-start=5",
                "--game-over-score=250",
                "--ai2=montecarlo:2000:2000:4",
                "--rotate=true",
                "--csv=results_heuristic-vs-montecarlo-2000-2000-4_100-seeds.csv",
                "--max-moves=20000"
            });

            Assert.That(options.WeightSets, Is.Empty);
            Assert.That(options.BaselineWeight, Is.EqualTo(1f));
            Assert.That(options.WeightStep, Is.EqualTo(0.25f));
        }

        [Test]
        public void HeuristicGeneticTuningArgumentParser_ShouldParseOptions()
        {
            var options = HeuristicGeneticTuningArgumentParser.Parse(new[]
            {
                "--mode=genetic",
                "--games=100",
                "--seed-start=3",
                "--game-over-score=250",
                "--ai2=montecarlo:2000:2000:4",
                "--rotate=true",
                "--csv=results.csv",
                "--max-moves=20000",
                "--generations=7",
                "--children-per-generation=15",
                "--parent-count=10",
                "--mutation-rate=0.5",
                "--mutation-min=-3",
                "--mutation-max=3",
                "--random-seed=99",
                "--baseline-weight=1.25"
            });

            Assert.That(options.Games, Is.EqualTo(100));
            Assert.That(options.SeedStart, Is.EqualTo(3));
            Assert.That(options.Generations, Is.EqualTo(7));
            Assert.That(options.ChildrenPerGeneration, Is.EqualTo(15));
            Assert.That(options.TopParentCount, Is.EqualTo(10));
            Assert.That(options.MutationRate, Is.EqualTo(0.5f));
            Assert.That(options.MutationDeltaMin, Is.EqualTo(-3f));
            Assert.That(options.MutationDeltaMax, Is.EqualTo(3f));
            Assert.That(options.RandomSeed, Is.EqualTo(99));
            Assert.That(options.BaselineWeight, Is.EqualTo(1.25f));
        }

        [Test]
        public void Run_WithSeatHeuristicWeights_ShouldCaptureWeightsInResult()
        {
            var options = new AiBenchmarkOptions
            {
                Games = 1,
                Players = 2,
                SeedStart = 1,
                GameOverScore = 40,
                Ai1Strategy = "normal",
                Ai2Strategy = "random",
                Ai1HeuristicOptions = new Haggis.AI.Strategies.HeuristicOptions
                {
                    ContinuationFollowUpWeight = 0.5f,
                    BombOpeningWeight = 0f
                },
                Rotate = false,
                MaxMovesPerGame = 2000
            };

            var result = new AiBenchmarkRunner().Run(options).Single();

            Assert.That(result.Completed, Is.True, result.Error);
            Assert.That(result.HeuristicWeightsByPlayer["p1"], Does.Contain("ContinuationFollowUpWeight=0.5"));
            Assert.That(result.HeuristicWeightsByPlayer["p1"], Does.Contain("BombOpeningWeight=0"));
            Assert.That(result.HeuristicWeightsByPlayer["p2"], Is.Empty);
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

            Assert.That(path.Replace('\\', '/'), Is.EqualTo("reports/results_normal-vs-montecarlo-800-100-4-vs-normal.csv"));
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
                Ai1Strategy = "normal",
                Ai2Strategy = "random",
                Ai3Strategy = "normal",
                Rotate = true,
                MaxMovesPerGame = 10000
            };

            var results = new AiBenchmarkRunner().Run(options);

            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(results[0].StrategiesByPlayer["p1"], Is.EqualTo("normal"));
            Assert.That(results[0].StrategiesByPlayer["p2"], Is.EqualTo("random"));
            Assert.That(results[0].StrategiesByPlayer["p3"], Is.EqualTo("normal"));
            Assert.That(results[1].StrategiesByPlayer["p1"], Is.EqualTo("normal"));
            Assert.That(results[1].StrategiesByPlayer["p2"], Is.EqualTo("random"));
            Assert.That(results[1].StrategiesByPlayer["p3"], Is.EqualTo("normal"));
            Assert.That(results[2].StrategiesByPlayer["p1"], Is.EqualTo("normal"));
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
                    MonteCarloDecisionCount = 2,
                    AverageTreeNodeCount = 123.5,
                    AverageTreeDepth = 7.5,
                    MaxTreeNodeCount = 180,
                    MaxTreeDepth = 10,
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
            Assert.That(summary.Format(), Does.Contain("mcts decisions"));
            Assert.That(summary.Format(), Does.Contain("avg tree nodes"));
            Assert.That(summary.Format(), Does.Contain("avg tree depth"));
            Assert.That(summary.Format(), Does.Contain("max tree nodes"));
            Assert.That(summary.Format(), Does.Contain("max tree depth"));
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
                Assert.That(header, Does.Contain("p1HeuristicWeights"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void HeuristicWeightsCsvWriter_ShouldWriteSeparateWeightsFile()
        {
            var path = Path.GetTempFileName();
            try
            {
                var options = new AiBenchmarkOptions
                {
                    Players = 2,
                    Ai1Strategy = "normal",
                    Ai2Strategy = "montecarlo:2000:2000:4",
                    Ai1HeuristicOptions = new Haggis.AI.Strategies.HeuristicOptions
                    {
                        ContinuationFollowUpWeight = 0.5f,
                        BombOpeningWeight = 0f
                    }
                };

                AiBenchmarkHeuristicWeightsCsvWriter.Write(path, options, new[]
                {
                    new AiBenchmarkGameResult
                    {
                        Completed = true,
                        Winner = "p1",
                        Scores = new System.Collections.Generic.Dictionary<string, int>
                        {
                            ["p1"] = 260,
                            ["p2"] = 210
                        }
                    },
                    new AiBenchmarkGameResult
                    {
                        Completed = true,
                        Winner = "p2",
                        Scores = new System.Collections.Generic.Dictionary<string, int>
                        {
                            ["p1"] = 180,
                            ["p2"] = 255
                        }
                    }
                });

                var lines = File.ReadAllLines(path);
                Assert.That(lines[0], Is.EqualTo("seat,player,strategy,heuristicWeights,wins,winRatePct,averageScore"));
                Assert.That(lines[1], Does.Contain("1,p1,normal"));
                Assert.That(lines[1], Does.Contain("ContinuationFollowUpWeight=0.5"));
                Assert.That(lines[1], Does.Contain("BombOpeningWeight=0"));
                Assert.That(lines[1], Does.Contain(",1,50.00,220.00"));
                Assert.That(lines[2], Does.Contain("2,p2,montecarlo:2000:2000:4"));
                Assert.That(lines[2], Does.Contain(",1,50.00,232.50"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void HeuristicTuningCsvWriter_ShouldCreateStableHeaderWithSplitWeightColumns()
        {
            var path = Path.GetTempFileName();
            try
            {
                HeuristicTuningCsvWriter.EnsureHeader(path);

                var header = File.ReadLines(path).First();

                Assert.That(header, Does.StartWith("games,seedStart,rotate,gameOverScore,maxMovesPerGame,monteCarloStrategy,"));
                Assert.That(header, Does.Contain("BombOpeningWeight"));
                Assert.That(header, Does.Contain("ContinuationFollowUpWeight"));
                Assert.That(header, Does.Contain("PlayableBombInEndgameWeight"));
                Assert.That(header, Does.Contain("heuristicWinRatePct"));
                Assert.That(header, Does.Contain("monteCarloAverageScore"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void HeuristicTuningCsvWriter_ShouldWriteOneRowPerCompletedSimulation()
        {
            var path = Path.GetTempFileName();
            try
            {
                HeuristicTuningCsvWriter.Append(path, new HeuristicTuningRunResult
                {
                    Games = 100,
                    SeedStart = 1,
                    Rotate = true,
                    GameOverScore = 250,
                    MaxMovesPerGame = 20000,
                    MonteCarloStrategy = "montecarlo:2000:2000:4",
                    HeuristicOptions = new HeuristicOptions
                    {
                        ContinuationFollowUpWeight = 0.5f,
                        BombOpeningWeight = 2f
                    },
                    CompletedGames = 200,
                    FailedGames = 0,
                    HeuristicWins = 112,
                    MonteCarloWins = 88,
                    HeuristicWinRatePct = 56,
                    MonteCarloWinRatePct = 44,
                    HeuristicAverageScore = 241.25,
                    MonteCarloAverageScore = 228.5,
                    AverageRounds = 5.5,
                    AverageMoves = 33.25,
                    AverageGameElapsedMs = 12.75,
                    TotalGameElapsedMs = 2550,
                    TimestampUtc = new System.DateTime(2026, 6, 22, 16, 0, 0, System.DateTimeKind.Utc)
                });

                var lines = File.ReadAllLines(path);

                Assert.That(lines, Has.Length.EqualTo(2));
                Assert.That(lines[1], Does.Contain("montecarlo:2000:2000:4"));
                Assert.That(lines[1], Does.Contain(",2,"));
                Assert.That(lines[1], Does.Contain(",0.5,"));
                Assert.That(lines[1], Does.Contain(",56.00,44.00,241.25,228.50,"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void HeuristicTuningRunner_ShouldSkipExistingIdenticalSimulationAndAppendMissingOne()
        {
            var path = Path.GetTempFileName();
            try
            {
                var existingWeights = new HeuristicOptions
                {
                    ContinuationFollowUpWeight = 1f,
                    BombOpeningWeight = 1f
                };
                var missingWeights = new HeuristicOptions
                {
                    ContinuationFollowUpWeight = 2f,
                    BombOpeningWeight = 0.5f
                };

                HeuristicTuningCsvWriter.Append(path, new HeuristicTuningRunResult
                {
                    Games = 100,
                    SeedStart = 1,
                    Rotate = true,
                    GameOverScore = 250,
                    MaxMovesPerGame = 20000,
                    MonteCarloStrategy = "montecarlo:2000:2000:4",
                    HeuristicOptions = existingWeights,
                    CompletedGames = 200,
                    FailedGames = 0,
                    HeuristicWins = 100,
                    MonteCarloWins = 100
                });

                var evaluator = new FakeHeuristicTuningEvaluator();
                var runner = new HeuristicTuningRunner(evaluator);

                var produced = runner.Run(new HeuristicTuningBatchOptions
                {
                    Games = 100,
                    SeedStart = 1,
                    Rotate = true,
                    GameOverScore = 250,
                    MaxMovesPerGame = 20000,
                    MonteCarloStrategy = "montecarlo:2000:2000:4",
                    CsvPath = path,
                    WeightSets = new[] { existingWeights, missingWeights }
                });

                Assert.That(evaluator.Calls, Is.EqualTo(1));
                Assert.That(produced, Has.Count.EqualTo(1));
                Assert.That(produced[0].HeuristicOptions.ContinuationFollowUpWeight, Is.EqualTo(2f));

                var lines = File.ReadAllLines(path);
                Assert.That(lines, Has.Length.EqualTo(3));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void HeuristicTuningRunner_ShouldResumeWhenMonteCarloStrategyAndWeightsMatch()
        {
            var path = Path.GetTempFileName();
            try
            {
                var weights = new HeuristicOptions
                {
                    ContinuationFollowUpWeight = 1f
                };

                HeuristicTuningCsvWriter.Append(path, new HeuristicTuningRunResult
                {
                    Games = 100,
                    SeedStart = 1,
                    Rotate = true,
                    GameOverScore = 250,
                    MaxMovesPerGame = 20000,
                    MonteCarloStrategy = "montecarlo:2000:2000:4",
                    HeuristicOptions = weights,
                    CompletedGames = 200,
                    FailedGames = 0,
                    HeuristicWins = 100,
                    MonteCarloWins = 100
                });

                var evaluator = new FakeHeuristicTuningEvaluator();
                var runner = new HeuristicTuningRunner(evaluator);

                var produced = runner.Run(new HeuristicTuningBatchOptions
                {
                    Games = 200,
                    SeedStart = 1,
                    Rotate = true,
                    GameOverScore = 250,
                    MaxMovesPerGame = 20000,
                    MonteCarloStrategy = "montecarlo:2000:2000:4",
                    CsvPath = path,
                    WeightSets = new[] { weights }
                });

                Assert.That(evaluator.Calls, Is.EqualTo(0));
                Assert.That(produced, Has.Count.EqualTo(0));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void HeuristicTuningRunner_WithoutWeightSets_ShouldTuneOneWeightAtATimeAndResetBetweenWeights()
        {
            var path = Path.GetTempFileName();
            try
            {
                var evaluator = new SequencedAutoTuneEvaluator();
                var runner = new HeuristicTuningRunner(evaluator);

                var produced = runner.Run(new HeuristicTuningBatchOptions
                {
                    Games = 100,
                    SeedStart = 1,
                    Rotate = true,
                    GameOverScore = 250,
                    MaxMovesPerGame = 20000,
                    MonteCarloStrategy = "montecarlo:2000:2000:4",
                    CsvPath = path,
                    BaselineWeight = 1f,
                    WeightStep = 0.25f,
                    WeightSets = new HeuristicOptions[0]
                });

                Assert.That(produced.Count, Is.GreaterThan(HeuristicOptionsSerializer.WeightPropertyNames.Count));
                Assert.That(evaluator.VisitedWeights[0], Is.EqualTo((1f, 1f)));
                Assert.That(evaluator.VisitedWeights[1], Is.EqualTo((1f, 1.25f)));
                Assert.That(evaluator.VisitedWeights[2], Is.EqualTo((1f, 1.5f)));
                Assert.That(evaluator.VisitedWeights[3], Is.EqualTo((1f, 0.75f)));
                Assert.That(evaluator.VisitedWeights[4], Is.EqualTo((1f, 0.5f)));
                Assert.That(evaluator.VisitedWeights[5], Is.EqualTo((1f, 0.25f)));
                Assert.That(evaluator.VisitedWeights[6], Is.EqualTo((1f, 0f)));
                Assert.That(evaluator.VisitedWeights[7], Is.EqualTo((1.25f, 1f)));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void HeuristicGeneticTuningRunner_ShouldUseTopParentsAndProduceChildren()
        {
            var path = Path.GetTempFileName();
            try
            {
                for (var index = 0; index < 12; index++)
                {
                    HeuristicTuningCsvWriter.Append(path, new HeuristicTuningRunResult
                    {
                        Games = 100,
                        SeedStart = 1,
                        Rotate = true,
                        GameOverScore = 250,
                        MaxMovesPerGame = 20000,
                        MonteCarloStrategy = "montecarlo:2000:2000:4",
                        HeuristicOptions = new HeuristicOptions
                        {
                            BombOpeningWeight = index < 10 ? 1f + index : 100f + index,
                            ContinuationFollowUpWeight = index < 10 ? 2f + index : 200f + index
                        },
                        CompletedGames = 200,
                        FailedGames = 0,
                        HeuristicWins = 100 - index,
                        MonteCarloWins = 100 + index,
                        HeuristicWinRatePct = 50 - index,
                        HeuristicAverageScore = 200 - index
                    });
                }

                var evaluator = new CapturingGeneticEvaluator();
                var runner = new HeuristicGeneticTuningRunner(evaluator, new Random(123), null);

                var produced = runner.Run(new HeuristicGeneticTuningOptions
                {
                    Games = 100,
                    SeedStart = 1,
                    Rotate = true,
                    GameOverScore = 250,
                    MaxMovesPerGame = 20000,
                    MonteCarloStrategy = "montecarlo:2000:2000:4",
                    CsvPath = path,
                    Generations = 1,
                    ChildrenPerGeneration = 3,
                    TopParentCount = 10,
                    MutationRate = 0f,
                    MutationDeltaMin = 0f,
                    MutationDeltaMax = 0f,
                    RandomSeed = 123,
                    BaselineWeight = 1f
                });

                Assert.That(produced, Has.Count.EqualTo(3));
                Assert.That(evaluator.CapturedWeights, Has.Count.EqualTo(3));
                Assert.That(evaluator.CapturedWeights.All(weight => weight.BombOpeningWeight < 100f), Is.True);
                Assert.That(evaluator.CapturedWeights.All(weight => weight.ContinuationFollowUpWeight < 200f), Is.True);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void HeuristicGeneticTuningRunner_ShouldBootstrapBaselineWhenCsvIsEmpty()
        {
            var path = Path.GetTempFileName();
            File.Delete(path);

            try
            {
                var evaluator = new CapturingGeneticEvaluator();
                var runner = new HeuristicGeneticTuningRunner(evaluator, new Random(7), null);

                var produced = runner.Run(new HeuristicGeneticTuningOptions
                {
                    Games = 100,
                    SeedStart = 1,
                    Rotate = true,
                    GameOverScore = 250,
                    MaxMovesPerGame = 20000,
                    MonteCarloStrategy = "montecarlo:2000:2000:4",
                    CsvPath = path,
                    Generations = 1,
                    ChildrenPerGeneration = 1,
                    TopParentCount = 10,
                    MutationRate = 0f,
                    MutationDeltaMin = 0f,
                    MutationDeltaMax = 0f,
                    RandomSeed = 7,
                    BaselineWeight = 1f
                });

                Assert.That(produced.Count, Is.GreaterThanOrEqualTo(1));
                Assert.That(evaluator.CapturedWeights[0].BombOpeningWeight, Is.EqualTo(1f));
                Assert.That(evaluator.CapturedWeights[0].ContinuationFollowUpWeight, Is.EqualTo(1f));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void HeuristicTuningEvaluator_ShouldAggregateCompletedGamesIntoSingleRunResult()
        {
            var benchmarkResults = new[]
            {
                new AiBenchmarkGameResult
                {
                    Completed = true,
                    Winner = "p1",
                    Rounds = 5,
                    Moves = 30,
                    GameElapsedMs = 10,
                    Scores = new System.Collections.Generic.Dictionary<string, int>
                    {
                        ["p1"] = 250,
                        ["p2"] = 200
                    }
                },
                new AiBenchmarkGameResult
                {
                    Completed = true,
                    Winner = "p2",
                    Rounds = 7,
                    Moves = 50,
                    GameElapsedMs = 30,
                    Scores = new System.Collections.Generic.Dictionary<string, int>
                    {
                        ["p1"] = 180,
                        ["p2"] = 255
                    }
                },
                new AiBenchmarkGameResult
                {
                    Completed = false,
                    Error = "failed"
                }
            };

            var result = new HeuristicTuningEvaluator(_ => benchmarkResults).Evaluate(
                new HeuristicTuningBatchOptions
                {
                    Games = 2,
                    SeedStart = 1,
                    Rotate = true,
                    GameOverScore = 250,
                    MaxMovesPerGame = 20000,
                    MonteCarloStrategy = "montecarlo:2000:2000:4"
                },
                new HeuristicOptions
                {
                    ContinuationFollowUpWeight = 0.5f
                });

            Assert.That(result.CompletedGames, Is.EqualTo(2));
            Assert.That(result.FailedGames, Is.EqualTo(1));
            Assert.That(result.HeuristicWins, Is.EqualTo(1));
            Assert.That(result.MonteCarloWins, Is.EqualTo(1));
            Assert.That(result.HeuristicWinRatePct, Is.EqualTo(50).Within(0.01));
            Assert.That(result.HeuristicAverageScore, Is.EqualTo(215).Within(0.01));
            Assert.That(result.MonteCarloAverageScore, Is.EqualTo(227.5).Within(0.01));
            Assert.That(result.AverageRounds, Is.EqualTo(6).Within(0.01));
            Assert.That(result.AverageMoves, Is.EqualTo(40).Within(0.01));
            Assert.That(result.TotalGameElapsedMs, Is.EqualTo(40));
        }

        private sealed class FakeHeuristicTuningEvaluator : IHeuristicTuningEvaluator
        {
            public int Calls { get; private set; }

            public HeuristicTuningRunResult Evaluate(HeuristicTuningBatchOptions batchOptions, HeuristicOptions heuristicOptions)
            {
                Calls++;
                return new HeuristicTuningRunResult
                {
                    Games = batchOptions.Games,
                    SeedStart = batchOptions.SeedStart,
                    Rotate = batchOptions.Rotate,
                    GameOverScore = batchOptions.GameOverScore,
                    MaxMovesPerGame = batchOptions.MaxMovesPerGame,
                    MonteCarloStrategy = batchOptions.MonteCarloStrategy,
                    HeuristicOptions = heuristicOptions,
                    CompletedGames = batchOptions.Rotate ? batchOptions.Games * 2 : batchOptions.Games,
                    FailedGames = 0,
                    HeuristicWins = 1,
                    MonteCarloWins = 0,
                    HeuristicWinRatePct = 100,
                    MonteCarloWinRatePct = 0,
                    HeuristicAverageScore = 250,
                    MonteCarloAverageScore = 200
                };
            }
        }

        private sealed class SequencedAutoTuneEvaluator : IHeuristicTuningEvaluator
        {
            public System.Collections.Generic.List<(float BombOpeningWeight, float BombContinuationWeight)> VisitedWeights { get; } =
                new System.Collections.Generic.List<(float BombOpeningWeight, float BombContinuationWeight)>();

            public HeuristicTuningRunResult Evaluate(HeuristicTuningBatchOptions batchOptions, HeuristicOptions heuristicOptions)
            {
                var bombOpeningWeight = heuristicOptions.BombOpeningWeight;
                var bombContinuationWeight = heuristicOptions.BombContinuationWeight;
                VisitedWeights.Add((bombOpeningWeight, bombContinuationWeight));

                double winRate = 50;
                double averageScore = 200;

                if (bombOpeningWeight > 1f && bombContinuationWeight == 1f)
                {
                    if (Math.Abs(bombOpeningWeight - 1.25f) < 0.001f)
                    {
                        winRate = 55;
                        averageScore = 210;
                    }
                    else if (Math.Abs(bombOpeningWeight - 1.5f) < 0.001f)
                    {
                        winRate = 54;
                        averageScore = 209;
                    }
                }

                if (bombContinuationWeight > 1f && bombOpeningWeight == 1f)
                {
                    if (Math.Abs(bombContinuationWeight - 1.25f) < 0.001f)
                    {
                        winRate = 53;
                        averageScore = 205;
                    }
                    else if (Math.Abs(bombContinuationWeight - 1.5f) < 0.001f)
                    {
                        winRate = 52;
                        averageScore = 204;
                    }
                }

                return new HeuristicTuningRunResult
                {
                    Games = batchOptions.Games,
                    SeedStart = batchOptions.SeedStart,
                    Rotate = batchOptions.Rotate,
                    GameOverScore = batchOptions.GameOverScore,
                    MaxMovesPerGame = batchOptions.MaxMovesPerGame,
                    MonteCarloStrategy = batchOptions.MonteCarloStrategy,
                    HeuristicOptions = HeuristicOptionsSerializer.Clone(heuristicOptions),
                    CompletedGames = batchOptions.Games * 2,
                    FailedGames = 0,
                    HeuristicWins = 1,
                    MonteCarloWins = 0,
                    HeuristicWinRatePct = winRate,
                    MonteCarloWinRatePct = 100 - winRate,
                    HeuristicAverageScore = averageScore,
                    MonteCarloAverageScore = 180
                };
            }
        }

        private sealed class CapturingGeneticEvaluator : IHeuristicTuningEvaluator
        {
            public System.Collections.Generic.List<HeuristicOptions> CapturedWeights { get; } =
                new System.Collections.Generic.List<HeuristicOptions>();

            public HeuristicTuningRunResult Evaluate(HeuristicTuningBatchOptions batchOptions, HeuristicOptions heuristicOptions)
            {
                var cloned = HeuristicOptionsSerializer.Clone(heuristicOptions);
                CapturedWeights.Add(cloned);

                return new HeuristicTuningRunResult
                {
                    Games = batchOptions.Games,
                    SeedStart = batchOptions.SeedStart,
                    Rotate = batchOptions.Rotate,
                    GameOverScore = batchOptions.GameOverScore,
                    MaxMovesPerGame = batchOptions.MaxMovesPerGame,
                    MonteCarloStrategy = batchOptions.MonteCarloStrategy,
                    HeuristicOptions = cloned,
                    CompletedGames = batchOptions.Games * 2,
                    FailedGames = 0,
                    HeuristicWins = 75,
                    MonteCarloWins = 25,
                    HeuristicWinRatePct = 75,
                    MonteCarloWinRatePct = 25,
                    HeuristicAverageScore = 240,
                    MonteCarloAverageScore = 180
                };
            }
        }

        private static long StopwatchTicksFromMs(double milliseconds)
        {
            return (long)(milliseconds * System.Diagnostics.Stopwatch.Frequency / 1000.0);
        }
    }
}
