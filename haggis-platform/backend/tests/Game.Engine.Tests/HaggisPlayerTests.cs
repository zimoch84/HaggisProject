using NUnit.Framework;
using Haggis.Domain.Extentions;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using Haggis.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaggisTests
{
    [TestFixture()]
    public class HaggisPlayerTests
    {

        IHaggisPlayer Piotr = new HaggisPlayer("Piotr");
        IHaggisPlayer Slawek = new HaggisPlayer("S³awek");
        IHaggisPlayer Robert = new HaggisPlayer("Robert");

        List<IHaggisPlayer> players;

        HaggisGame Game;

        HaggisGameState GameState;

        [SetUp]
        public void SetUp()
        {
            Piotr.Hand  = new List<string> { "2Y", "3Y" }.ToCards();
            Slawek.Hand = new List<string> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<string> { "3O", "3B" }.ToCards();

            players = new List<IHaggisPlayer>() { Piotr, Slawek, Robert };

            GameState = new HaggisGameState(players);
            GameState.SetCurrentPlayer(Piotr);
        }

        [Test]
        public void CloneTest()
        {
            var piotrClone = (IHaggisPlayer)Piotr.Clone();

            Assert.That(piotrClone != Piotr, Is.True);
            Assert.That(piotrClone.Name.Equals(Piotr.Name), Is.True);

        }

        [Test]
        public void PossibleTricksTest_Is_Final()
        {
            var tricks = Robert.SuggestedTricks("2BO_PAIR".ToTrick());

            Assert.That(tricks.Count == 1, Is.True);
            Assert.That(tricks.Last().Equals("3BO_PAIR".ToTrick()), Is.True);
            Assert.That(tricks.Last().IsFinal, Is.True);

            Trick trick = null;
            tricks = Robert.SuggestedTricks(trick);

            Assert.That(tricks.Count == 1, Is.True);
            Assert.That(tricks.Last().Equals("3BO_PAIR".ToTrick()), Is.True);
            Assert.That(tricks.Last().IsFinal, Is.True);

        }

        [TestCase(new string[] { "3Y", "5O", "7Y", "9B" }, 4)]
        [TestCase(new string[] { "3Y", "5O", "7G", "9B" }, 1)] //because if final
        [TestCase(new string[] { "3Y", "5O", "7G", "9B", "10O" }, 6)] 

        public void CheckIfAllPossibleTricksAreCorrect(string[] cards, int expectedTricks) {
            Piotr.Hand = cards.ToCards();
            Trick lastTrick = null;

            var possibleTricks =  Piotr.SuggestedTricks(lastTrick);
            Assert.That(possibleTricks.Count == expectedTricks, Is.True);  
        }
        
        [TestCase("2O_SINGLE", new string[] { "3Y", "5O", "7Y", "9B" }, 4)]
        [TestCase("2O_SINGLE", new string[] { "3Y", "5O", "7G", "9B" }, 1)] //because bomb is final
        [TestCase("2O_SINGLE", new string[] { "3Y", "5O", "7G", "9B", "10O" }, 6)] 
        
        [TestCase("5O_SINGLE", new string[] { "3Y", "5O", "7Y", "9B" }, 2)]
        [TestCase("5O_SINGLE", new string[] { "3Y", "5O", "7G", "9B" }, 1)] //because bomb is final
        [TestCase("5O_SINGLE", new string[] { "3Y", "5O", "7G", "9B", "10O" }, 4)] 

        public void CheckIfAllPossibleTricksAreCorrectIfLastTrickWas(string lastTrick, string[] cards, int expectedTricks) {
            Piotr.Hand = cards.ToCards();
            Trick _lasttrick = lastTrick.ToTrick();

            var possibleTricks =  Piotr.SuggestedTricks(_lasttrick);
            Assert.That(possibleTricks.Count == expectedTricks, Is.True);  
        }


        [Test]
        public void CheckIfSEQ3IsNotPossibleWhenOnlyWilds()
        {
            Piotr.Hand = new List<string> { "J", "Q", "K" }.ToCards();
            GameState = new HaggisGameState(new List<IHaggisPlayer> { Piotr, Slawek, Robert });

            var tricks = Piotr.AllPossibleTricks(TrickType.SEQ3);

            foreach (var trick in tricks)
            {
                Assert.That(trick.Type, Is.Not.EqualTo(TrickType.SEQ3));
            }
        }

        [Test]
        public void ShouldSuggestWildedTrickWhenHaveOneCardLessToPlayingTrick() 
        {
            Slawek.Hand = new List<string> { "2G", "4G", "J" }.ToCards();

            var suggestedTricks = Slawek.SuggestedTricks("3GO_PAIR".ToTrick());

            Assert.That(suggestedTricks.Count, Is.EqualTo(1));
        }


    }
}