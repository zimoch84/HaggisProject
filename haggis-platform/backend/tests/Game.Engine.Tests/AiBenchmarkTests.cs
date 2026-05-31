using System.Linq;
using Haggis.AI.Benchmark;
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
                Strategy = "normal",
                Opponent = "normal",
                Rotate = false,
                MaxMovesPerGame = 2000
            };

            var result = new AiBenchmarkRunner().Run(options).Single();

            Assert.That(result.Completed, Is.True, result.Error);
            Assert.That(result.Moves, Is.GreaterThan(0));
            Assert.That(result.Rounds, Is.GreaterThan(0));
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
                Strategy = "normal",
                Opponent = "random",
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
        public void ArgumentParser_ShouldUseDefaultsAndOverrideValues()
        {
            var defaults = AiBenchmarkArgumentParser.Parse(new string[0]);
            var custom = AiBenchmarkArgumentParser.Parse(new[]
            {
                "--games=12",
                "--strategy=random",
                "--opponent=montecarlo-fast",
                "--log=games.log"
            });

            Assert.That(defaults.Games, Is.EqualTo(1000));
            Assert.That(defaults.Strategy, Is.EqualTo("normal"));
            Assert.That(defaults.Opponent, Is.EqualTo("normal"));
            Assert.That(custom.Games, Is.EqualTo(12));
            Assert.That(custom.Strategy, Is.EqualTo("random"));
            Assert.That(custom.Opponent, Is.EqualTo("montecarlo-fast"));
            Assert.That(custom.LogPath, Is.EqualTo("games.log"));
        }

        [Test]
        public void OutputPath_ShouldAppendTimestampAndStrategiesBeforeExtension()
        {
            var options = new AiBenchmarkOptions
            {
                Strategy = "normal",
                Opponent = "montecarlo-fast"
            };

            var path = AiBenchmarkOutputPath.WithRunSuffix(
                "reports/results.csv",
                options,
                new System.DateTime(2026, 5, 31, 14, 5, 6, 789));

            Assert.That(path.Replace('\\', '/'), Is.EqualTo("reports/results_20260531_140506_normal-vs-montecarlo-fast.csv"));
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
                Strategy = "normal",
                Opponent = "random",
                Rotate = false,
                MaxMovesPerGame = 2000
            };

            var result = new AiBenchmarkRunner().Run(options).Single();

            Assert.That(result.LogLines, Has.Some.Contains("GAME seed=1 rotation=0"));
            Assert.That(result.LogLines, Has.Some.Contains("ROUND 1 START current=p1"));
            Assert.That(result.LogLines, Has.Some.Contains("hand p1: 2R 2B 2G"));
            Assert.That(result.LogLines, Has.Some.Contains("TRICK 1 START current=p1"));
            Assert.That(result.LogLines, Has.Some.Contains("move 1: round=1 trick=1"));
            Assert.That(result.LogLines, Has.Some.Contains("GAME END"));
        }

        [Test]
        public void Run_WithRotation_ShouldStartEachGameFromSeatOne()
        {
            var options = new AiBenchmarkOptions
            {
                Games = 1,
                Players = 3,
                SeedStart = 3,
                GameOverScore = 40,
                Strategy = "normal",
                Opponent = "random",
                Rotate = true,
                MaxMovesPerGame = 10000
            };

            var results = new AiBenchmarkRunner().Run(options);

            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(
                results.All(result => result.LogLines.Any(line => line.Contains("ROUND 1 START current=p1"))),
                Is.True);
        }
    }
}
