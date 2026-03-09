using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.AI.Strategies;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Haggis.Domain.Extentions.CardsExtensions;
using static Haggis.Domain.Model.HaggisAction;
using Haggis.AI.Model;

namespace HaggisTests.Strategies
{
    public class PlayMonteCarloStrategyTests
    {

        public MonteCarloStrategy strategy;
        RoundState GameState;
        AIPlayer Piotr;
        AIPlayer Slawek;
        AIPlayer Robert;

        List<IHaggisPlayer> _players;

        [SetUp]

        public void Setup() {

            strategy = new MonteCarloStrategy(1,1L);

            Piotr = new AIPlayer("Piotr");
            Slawek = new AIPlayer("Sławek");
            Robert = new AIPlayer("Robert");

            Piotr.Hand = Cards("2Y", "3Y");
            Slawek.Hand = Cards("2G", "4G");
            Robert.Hand = Cards("2O", "2B");

            _players = new List<IHaggisPlayer> { Piotr, Slawek, Robert };

            GameState = new RoundState(_players);

        }

        [Test]
        public  void ShouldNotChanteGameStateAfterGettinggAction()
        {
            var currentPlayerName = GameState.CurrentPlayer.Name;
            var nextPlayerName = GameState.NextPlayer.Name;
            var players = GameState.Players.DeepCopy();

            var actions = MonteCarloStrategy.GetTopActions(GameState, 2, 1);

            Assert.That(GameState.CurrentPlayer.Name, Is.EqualTo(currentPlayerName));
            Assert.That(GameState.NextPlayer.Name, Is.EqualTo(nextPlayerName));

            Assert.That(GameState.Players, Is.EqualTo(players));
        }
        [Repeat(10)]
        [TestCase(1000, 1000L)]
        public  void ShouldNotGetExceptionAfterXRuns(int runs, long timeBudget)
        {
            var game = new HaggisGame(_players);

            var gameState = new RoundState(_players);

            var currentPlayerName = gameState.CurrentPlayer.Name;
            var nextPlayerName = gameState.NextPlayer.Name;
            var players = gameState.Players.DeepCopy();

            var actions = MonteCarloStrategy.GetTopActions(gameState, runs, timeBudget);

            Assert.That(gameState.CurrentPlayer.Name, Is.EqualTo(currentPlayerName));
            Assert.That(gameState.NextPlayer.Name, Is.EqualTo(nextPlayerName));

            Assert.That(gameState.Players, Is.EqualTo(players));
        }

        [TestCase(100, 100L)]
        public void ShouldRunTillGameOver(int maxIteration, long timeBudget)
        {
            var game = new HaggisGame(_players);
            var gameState = new RoundState(_players);

            while (!gameState.RoundOver())
            {
                var actions = MonteCarloStrategy.GetTopActions(gameState, maxIteration, timeBudget);
                HaggisAction action = actions.First().Action;
                gameState.ApplyAction(action);
                Trace.WriteLine(action.ToString());
            }
        }

    }
}

