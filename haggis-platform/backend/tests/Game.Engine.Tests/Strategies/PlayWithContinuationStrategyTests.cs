using Haggis.Extentions;
using Haggis.Interfaces;
using Haggis.Model;
using Haggis.Strategies;
using System.Diagnostics;
using Haggis.StartingTrickFilterStrategies;
using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;
using System;

namespace HaggisTests.Strategies
{
    internal class PlayWithContinuationStrategyTests
    {

        IHaggisPlayer Piotr = new HaggisPlayer("Piotr");
        IHaggisPlayer Slawek = new HaggisPlayer("Sławek");
        IHaggisPlayer Robert = new HaggisPlayer("Robert");
        HaggisGameState gameState;


        [SetUp]
        public void SetUp() {

            Piotr.Hand = new List<String> { "2Y", "3Y", "4B", "4O", "4G", "5B", "5O", "6O", "7Y","7O","7G", "J" }.ToCards();
            Slawek.Hand = new List<String> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<String> { "3O", "3B" }.ToCards();
            gameState = new HaggisGameState(new List<IHaggisPlayer> { Piotr, Slawek, Robert });

        }
        [Test]
        public void ShouldGiveActionWithoutWilds() {

            var strategy = new ContinuationStrategy(false,  true);

            var action = strategy.GetPlayingAction(gameState);

            var trick = new Trick(Haggis.Enums.TrickType.PAIR, new string[] { "4B", "4O" }.ToCards());
            Assert.That(action.Trick, Is.EqualTo(trick));
        }


        [Test]
        public void ShouldAlwaysGiveSomeActions()
        {

            Piotr.Hand = new List<String> { "2Y", "3Y", "4B", "5B", "6O", "7Y", "7O", "7G", "J" }.ToCards();
            Slawek.Hand = new List<String> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<String> { "3O", "3B" }.ToCards();

            gameState = new HaggisGameState(new List<IHaggisPlayer> { Piotr, Slawek, Robert });

            var strategy = new ContinuationStrategy(false, true);

            var action = strategy.GetPlayingAction(gameState);

            var trick = new Trick(Haggis.Enums.TrickType.SINGLE, new string[] { "2Y" }.ToCards());
            Assert.That(action.Trick, Is.EqualTo(trick));
        }


        [Test]
        public void ShouldAlwaysGiveSomeActions_2()
        {

            Piotr.Hand = new List<String> { "2Y", "3Y", "4B", "5B", "6O", "7Y", "7O", "7G", "J" }.ToCards();
            Slawek.Hand = new List<String> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<String> { "3O", "3B" }.ToCards();

            gameState = new HaggisGameState(new List<IHaggisPlayer> { Slawek, Piotr, Robert });

            var strategy = new ContinuationStrategy(false, true);

            var action = strategy.GetPlayingAction(gameState);

            var trick = new Trick(Haggis.Enums.TrickType.SINGLE, new string[] { "2G" }.ToCards());
            Assert.That(action.Trick, Is.EqualTo(trick));
        }

        [TestCase(300, 1000l)]
        public void ShouldRunTillGameOver(int maxIteration, long timeBudget)
        {
           
            AIPlayer piotr = new AIPlayer("PiotrAI");
            AIPlayer slawek = new AIPlayer("SławekAI");
            AIPlayer robert = new AIPlayer("RobertAIs");

           

            piotr.PlayStrategy = new ContinuationStrategy(false, true);
            slawek.PlayStrategy = new ContinuationStrategy(false, true);
            robert.PlayStrategy = new ContinuationStrategy(false, true);

            piotr.StartingTrickFilterStrategy = new FilterContinuations(8, false);
            slawek.StartingTrickFilterStrategy = new FilterContinuations(8, false);
            robert.StartingTrickFilterStrategy = new FilterContinuations(8, false);


            var game = new HaggisGame(new List<IHaggisPlayer>{piotr,slawek,robert});
            game.NewRound();

            var gameState = new HaggisGameState(new List<IHaggisPlayer> { piotr, slawek, robert });

            while (!gameState.RoundOver())
            {
                var actions = MonteCarloStrategy.GetTopActions(gameState, maxIteration);
                HaggisAction action = actions.First().Action;
                gameState.ApplyAction(action);
                Trace.WriteLine(action.ToString());
            }
        }
    }
}
