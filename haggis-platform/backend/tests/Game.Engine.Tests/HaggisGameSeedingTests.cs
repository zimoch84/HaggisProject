using System.Collections.Generic;
using System.Linq;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests
{
    [TestFixture]
    public class HaggisGameSeedingTests
    {
        [Test]
        public void NewRound_WithSameSeed_ShouldDealSameHands()
        {
            var gameAPlayers = CreatePlayers();
            var gameBPlayers = CreatePlayers();

            var gameA = new HaggisGame(gameAPlayers);
            var gameB = new HaggisGame(gameBPlayers);

            gameA.SetSeed(12345);
            gameB.SetSeed(12345);

            gameA.NewRound();
            gameB.NewRound();

            var handsA = gameAPlayers.Select(ToHandSignature).ToArray();
            var handsB = gameBPlayers.Select(ToHandSignature).ToArray();

            Assert.That(handsA, Is.EqualTo(handsB));
        }

        [Test]
        public void NewRound_WithSameSeed_ShouldSelectSameStartingPlayer()
        {
            var gameAPlayers = CreatePlayers();
            var gameBPlayers = CreatePlayers();

            var gameA = new HaggisGame(gameAPlayers);
            var gameB = new HaggisGame(gameBPlayers);

            gameA.SetSeed(12345);
            gameB.SetSeed(12345);

            var stateA = gameA.NewRound();
            var stateB = gameB.NewRound();

            Assert.That(stateA.CurrentPlayer.Name, Is.EqualTo(stateB.CurrentPlayer.Name));
        }

        [Test]
        public void NewRound_WithDifferentSeeds_ShouldDealDifferentHands()
        {
            var gameAPlayers = CreatePlayers();
            var gameBPlayers = CreatePlayers();

            var gameA = new HaggisGame(gameAPlayers);
            var gameB = new HaggisGame(gameBPlayers);

            gameA.SetSeed(111);
            gameB.SetSeed(222);

            gameA.NewRound();
            gameB.NewRound();

            var handsA = gameAPlayers.Select(ToHandSignature).ToArray();
            var handsB = gameBPlayers.Select(ToHandSignature).ToArray();

            Assert.That(handsA, Is.Not.EqualTo(handsB));
        }

        [Test]
        public void NewRound_ShouldReturnInitializedGameState()
        {
            var players = CreatePlayers();
            var game = new HaggisGame(players);
            game.SetSeed(12345);

            var state = game.NewRound();

            Assert.That(state, Is.Not.Null);
            Assert.That(state.Players.Count, Is.EqualTo(3));
            Assert.That(state.Players.All(player => player.Hand.Count == 17), Is.True);
            Assert.That(state.RoundNumber, Is.EqualTo(1));
            Assert.That(state.MoveIteration, Is.EqualTo(0));
        }

        [Test]
        public void NewRound_ShouldIncreaseRoundNumber()
        {
            var players = CreatePlayers();
            var game = new HaggisGame(players);
            game.SetSeed(12345);

            var firstRound = game.NewRound();
            var secondRound = game.NewRound();

            Assert.That(firstRound.RoundNumber, Is.EqualTo(1));
            Assert.That(secondRound.RoundNumber, Is.EqualTo(2));
        }

        private static List<IHaggisPlayer> CreatePlayers()
        {
            return new List<IHaggisPlayer>
            {
                new HaggisPlayer("P1"),
                new HaggisPlayer("P2"),
                new HaggisPlayer("P3")
            };
        }

        private static string ToHandSignature(IHaggisPlayer player)
        {
            return string.Join("|", player.Hand.Select(card => card.ToString()));
        }
    }
}
