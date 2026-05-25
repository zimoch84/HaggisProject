using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static Haggis.Domain.Extentions.CardsExtensions;

namespace HaggisTests
{
    public class ScoringTableTests
    {
        [Test]
        public void RegisterRoundScoringResult_ShouldAppendRoundEntryToGameScoringTable()
        {
            var players = new List<IHaggisPlayer>
            {
                new HaggisPlayer("Alice") { Discard = Cards("J"), Score = 0 },
                new HaggisPlayer("Bob") { Discard = Cards("5O"), Score = 0 },
                new HaggisPlayer("Carol") { Discard = Cards("2G"), Score = 0 }
            };

            var game = new HaggisGame(players, new ClassicHaggisScoringStrategy());
            var state = game.NewRound();
            var service = new RoundEndScoringService();
            state.Players[0].Hand = Cards("2Y");
            state.Players[1].Hand = new List<Card>();
            state.Players[2].Hand = new List<Card>();

            service.Apply(state);
            game.RegisterRoundScoringResult(state);

            Assert.That(game.ScoringTable.Count, Is.EqualTo(1));
            Assert.That(game.ScoringTable[0].RoundNumber, Is.EqualTo(1));
            Assert.That(game.ScoringTable[0].WinnerPlayerName, Is.EqualTo("Alice"));
            Assert.That(game.GetPreviousRoundWinnerName(), Is.EqualTo("Alice"));
            var expectedLeastPointsPlayer = game.ScoringTable.GetPlayersTotalPoints()
                .OrderBy(score => score.Value)
                .ThenBy(score => score.Key)
                .First()
                .Key;
            Assert.That(game.ScoringTable.GetPlayerWithLeastPoints(), Is.EqualTo(expectedLeastPointsPlayer));
        }

        [Test]
        public void NewRound_ShouldKeepScoringTableInsideGame()
        {
            var players = new List<IHaggisPlayer>
            {
                new HaggisPlayer("Alice") { Discard = Cards("J"), Score = 0 },
                new HaggisPlayer("Bob") { Discard = Cards("5O"), Score = 0 },
                new HaggisPlayer("Carol") { Discard = Cards("2G"), Score = 0 }
            };

            var game = new HaggisGame(players, new ClassicHaggisScoringStrategy());
            game.SetSeed(42);
            var stateRound1 = game.NewRound();
            stateRound1.Players[0].Hand = Cards("2Y");
            stateRound1.Players[1].Hand = new List<Card>();
            stateRound1.Players[2].Hand = new List<Card>();

            stateRound1.Players[0].Discard = Cards("J");
            stateRound1.Players[1].Discard = Cards("5O");
            stateRound1.Players[2].Discard = Cards("2G");
            new RoundEndScoringService().Apply(stateRound1);
            game.RegisterRoundScoringResult(stateRound1);

            var stateRound2 = game.NewRound();
            var expectedStartingPlayer = game.ScoringTable.GetPlayerWithLeastPoints();
            var seatingOrder = new[] { "Alice", "Bob", "Carol" };
            var startIndex = System.Array.IndexOf(seatingOrder, expectedStartingPlayer);
            var expectedOrder = new[]
            {
                seatingOrder[startIndex],
                seatingOrder[(startIndex + 1) % seatingOrder.Length],
                seatingOrder[(startIndex + 2) % seatingOrder.Length]
            };

            Assert.That(stateRound2.RoundNumber, Is.EqualTo(2));
            Assert.That(game.ScoringTable.Count, Is.EqualTo(1));
            Assert.That(game.GetPreviousRoundWinnerName(), Is.EqualTo("Alice"));
            Assert.That(stateRound2.CurrentPlayer.Name, Is.EqualTo(expectedStartingPlayer));
            Assert.That(stateRound2.Players.Select(player => player.Name).ToArray(), Is.EqualTo(expectedOrder));
        }

        [Test]
        public void NewRound_ShouldResetPlayerRoundScoresToZero()
        {
            var players = new List<IHaggisPlayer>
            {
                new HaggisPlayer("Alice") { Score = 10 },
                new HaggisPlayer("Bob") { Score = 5 },
                new HaggisPlayer("Carol") { Score = 1 }
            };

            var game = new HaggisGame(players, new ClassicHaggisScoringStrategy());

            var state = game.NewRound();

            Assert.That(state.Players.All(player => player.Score == 0), Is.True);
        }

        [Test]
        public void GameOver_ShouldUseSummedPointsFromScoringTable()
        {
            var players = new List<IHaggisPlayer>
            {
                new HaggisPlayer("Alice"),
                new HaggisPlayer("Bob"),
                new HaggisPlayer("Carol")
            };

            var game = new HaggisGame(players, new ClassicHaggisScoringStrategy(gameOverScore: 10));
            game.ScoringTable.AddRoundScore(new RoundScoringResult
            {
                RoundNumber = 1,
                WinnerPlayerName = "Alice",
                PlayerScores = new List<PlayerRoundScore>
                {
                    new PlayerRoundScore { PlayerName = "Alice", TricksPoints = 6 },
                    new PlayerRoundScore { PlayerName = "Bob", TricksPoints = 3 },
                    new PlayerRoundScore { PlayerName = "Carol", TricksPoints = 1 }
                }
            });
            game.ScoringTable.AddRoundScore(new RoundScoringResult
            {
                RoundNumber = 2,
                WinnerPlayerName = "Bob",
                PlayerScores = new List<PlayerRoundScore>
                {
                    new PlayerRoundScore { PlayerName = "Alice", TricksPoints = 5 },
                    new PlayerRoundScore { PlayerName = "Bob", TricksPoints = 2 },
                    new PlayerRoundScore { PlayerName = "Carol", TricksPoints = 1 }
                }
            });

            var totals = game.ScoringTable.GetPlayersTotalPoints();

            Assert.That(totals["Alice"], Is.EqualTo(11));
            Assert.That(game.ScoringTable.GetPlayerWithLeastPoints(), Is.EqualTo("Carol"));
            Assert.That(game.GameOver(), Is.True);
        }

        [Test]
        public void BuildRoundScoringResult_ShouldIncludeDiscardRunOutAndHaggisPointsForFirstFinisher()
        {
            var players = new List<IHaggisPlayer>
            {
                new HaggisPlayer("Alice"),
                new HaggisPlayer("Bob"),
                new HaggisPlayer("Carol")
            };

            var strategy = new EveryCardOnePointScoringStrategy();
            var game = new HaggisGame(players, strategy);
            game.SetSeed(77);
            var state = game.NewRound();

            var firstFinished = state.Players.First(player => player.Name == "Bob");
            firstFinished.Hand = new List<Card>();
            firstFinished.Discard = Cards("2Y", "3Y");
            var others = state.Players.Where(player => player.Name != "Bob").ToList();
            others[0].Hand = Cards("4G", "5G");
            others[1].Hand = Cards("6O");

            new RunOutScoringService().Apply(state, HaggisAction.Pass(firstFinished));
            var roundResult = new ScoringTableService().BuildRoundScoringResult(state);

            var expectedRunOutPoints = (others[0].Hand.Count + others[1].Hand.Count) * strategy.RunOutMultiplier;
            var expectedDiscardPoints = firstFinished.Discard.Sum(card => strategy.GetCardPoints(card));
            var expectedHaggisCardsScore = state.HaggisCards.Sum(card => strategy.GetCardPoints(card));
            var bobRoundScore = roundResult.PlayerScores.First(score => score.PlayerName == "Bob");
            Assert.That(firstFinished.OpponentRemainingCardsOnFinish, Is.EqualTo(3));
            Assert.That(bobRoundScore.TricksPoints, Is.EqualTo(expectedDiscardPoints));
            Assert.That(bobRoundScore.OpponentsRemainingCardsPoints, Is.EqualTo(expectedRunOutPoints));
            Assert.That(bobRoundScore.HaggisPoints, Is.EqualTo(expectedHaggisCardsScore));
            Assert.That(bobRoundScore.RoundPoints, Is.EqualTo(expectedDiscardPoints + expectedRunOutPoints + expectedHaggisCardsScore));
            Assert.That(roundResult.FinishingOrderPlayerNames.First(), Is.EqualTo("Bob"));
        }

        [Test]
        public void BuildRoundScoringResult_ShouldSplitPointsIntoTricksRemainingCardsAndHaggis()
        {
            var players = new List<IHaggisPlayer>
            {
                new HaggisPlayer("Alice"),
                new HaggisPlayer("Bob")
            };

            var strategy = new EveryCardOnePointScoringStrategy();
            var state = new RoundState(players, strategy, roundNumber: 3, haggisCards: Cards("J", "Q").ToList());

            var alice = state.Players.First(player => player.Name == "Alice");
            var bob = state.Players.First(player => player.Name == "Bob");

            alice.Discard = Cards("2Y", "3Y", "4Y");
            alice.Hand = new List<Card>();
            alice.OpponentRemainingCardsOnFinish = -1;

            bob.Discard = Cards("5G");
            bob.Hand = Cards("6G", "7G", "8G", "9G", "10G");

            new RunOutScoringService().Apply(state, HaggisAction.Pass(alice));

            var result = new ScoringTableService().BuildRoundScoringResult(state);

            var aliceScore = result.PlayerScores.First(score => score.PlayerName == "Alice");
            var bobScore = result.PlayerScores.First(score => score.PlayerName == "Bob");
            var expectedHaggisPoints = state.HaggisCards.Sum(card => strategy.GetCardPoints(card));

            Assert.That(aliceScore.TricksPoints, Is.EqualTo(3));
            Assert.That(aliceScore.OpponentsRemainingCardsPoints, Is.EqualTo(5 * strategy.RunOutMultiplier));
            Assert.That(aliceScore.HaggisPoints, Is.EqualTo(expectedHaggisPoints));
            Assert.That(aliceScore.RoundPoints, Is.EqualTo(3 + 5 * strategy.RunOutMultiplier + expectedHaggisPoints));

            Assert.That(bobScore.TricksPoints, Is.EqualTo(1));
            Assert.That(bobScore.OpponentsRemainingCardsPoints, Is.EqualTo(0));
            Assert.That(bobScore.HaggisPoints, Is.EqualTo(0));
            Assert.That(bobScore.RoundPoints, Is.EqualTo(1));
        }
    }
}
