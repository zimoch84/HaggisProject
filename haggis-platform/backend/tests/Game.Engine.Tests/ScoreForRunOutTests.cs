using NUnit.Framework;
using Haggis.Model;
using Haggis.Extentions;
using Haggis.Interfaces;
using System.Collections.Generic;

namespace HaggisTests
{
    [TestFixture]
    public class ScoreForRunOutTests
    {
        [Test]
        public void ScoreForRunOut_AppliesScore_WhenPlayerFinished()
        {
            // Arrange
            var p1 = new HaggisPlayer("P1");
            var p2 = new HaggisPlayer("P2");
            var p3 = new HaggisPlayer("P3");

            // p1 finished
            p1.Hand = new List<Card>();

            // others have some cards
            p2.Hand = new List<Card> { "2Y".ToCard(), "3Y".ToCard() };
            p3.Hand = new List<Card> { "2G".ToCard() };

            var players = new List<IHaggisPlayer> { p1, p2, p3 };

            var action = HaggisAction.Pass(p1);

            // Act
            var applied = action.ScoreForRunOut(players, Haggis.Model.HaggisGame.HAND_RUNS_OUT_MULTIPLAYER);

            // Assert
            Assert.That(applied, Is.True);
            var expected = (p2.Hand.Count + p3.Hand.Count) * Haggis.Model.HaggisGame.HAND_RUNS_OUT_MULTIPLAYER;
            Assert.That(p1.Score, Is.EqualTo(expected));
        }

        [Test]
        public void ScoreForRunOut_DoesNotApply_WhenPlayerNotFinished()
        {
            var p1 = new HaggisPlayer("P1");
            var p2 = new HaggisPlayer("P2");

            p1.Hand = new List<Card> { "2Y".ToCard() };
            p2.Hand = new List<Card> { "2G".ToCard() };

            var players = new List<IHaggisPlayer> { p1, p2 };
            var action = HaggisAction.Pass(p1);



            var applied = action.ScoreForRunOut(players, Haggis.Model.HaggisGame.HAND_RUNS_OUT_MULTIPLAYER);

            Assert.That(applied, Is.False);
            Assert.That(p1.Score, Is.EqualTo(0));
        }

        [Test]
        public void ScoreForRunOut_ReturnsFalse_WhenPlayerNotFound()
        {
            var p1 = new HaggisPlayer("P1");
            var players = new List<IHaggisPlayer>();
            var action = HaggisAction.Pass(p1);

            var applied = action.ScoreForRunOut(players, Haggis.Model.HaggisGame.HAND_RUNS_OUT_MULTIPLAYER);

            Assert.That(applied, Is.False);
        }
    }
}
