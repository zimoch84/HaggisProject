using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace HaggisTests
{

    [TestFixture]
    internal class TrickPlayTests
    {

        HaggisPlayer Piotr;
        HaggisPlayer Slawek;
        HaggisPlayer Robert;

        [SetUp]
        public void SetUp()
        {


            Piotr = new HaggisPlayer("Piotr");
            Slawek = new HaggisPlayer("S³awek");
            Robert = new HaggisPlayer("Robert");

            Piotr.Hand = new List<String> { "2Y", "3Y" }.ToCards();
            Slawek.Hand = new List<String> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<String> { "2O", "2B" }.ToCards();


        }

        [Test]
        public void IsEndingPass_2_PlayersAfter_1Pass()
        {
            var trickPlay = new TrickPlay(2);
            trickPlay.AddAction(HaggisAction.Pass(new HaggisPlayer("Piotr")));
            Assert.That(trickPlay.IsEndingPass(), Is.True);

        }

        [Test]
        public void IsEndingPass_3_PlayersAfter_1Pass()
        {
            var trickPlay = new TrickPlay(3);
            trickPlay.AddAction(HaggisAction.Pass(Piotr));
            Assert.That(trickPlay.IsEndingPass(), Is.False);

        }

        [Test]
        public void IsEndingPass_3_PlayersAfter_1Pass_1Action_2Pass()
        {
            var trickPlay = new TrickPlay(3);

            trickPlay.AddAction(HaggisAction.Pass(Piotr));
            Assert.That(trickPlay.IsEndingPass(), Is.False);
            trickPlay.AddAction(HaggisAction.FromTrick("4BYOG_QUAD".ToTrick(), Piotr));
            Assert.That(trickPlay.IsEndingPass(), Is.False);
            trickPlay.AddAction(HaggisAction.Pass(Slawek));
            Assert.That(trickPlay.IsEndingPass(), Is.False);
            trickPlay.AddAction(HaggisAction.Pass(Piotr));
            Assert.That(trickPlay.IsEndingPass(), Is.True);

        }

        [Test]
        public void IsEndingPass_3_PlayersAfter_2Action_1Pass()
        {
            var trickPlay = new TrickPlay(3);
            trickPlay.AddAction(HaggisAction.FromTrick("4BYOG_QUAD", Piotr));
            Assert.That(trickPlay.IsEndingPass(), Is.False);
            trickPlay.AddAction(HaggisAction.FromTrick("4BYOG_QUAD", Slawek));
            Assert.That(trickPlay.IsEndingPass(), Is.False);
            trickPlay.AddAction(HaggisAction.Pass(Robert));
            Assert.That(trickPlay.IsEndingPass(), Is.False);
            trickPlay.AddAction(HaggisAction.Pass(Piotr));
            Assert.That(trickPlay.IsEndingPass(), Is.True);

        }
        /*
         *  Piotr.NextPlayer = Slawek.GUID;
            Slawek.NextPlayer = Robert.GUID;
            Robert.NextPlayer = Piotr.GUID;
         */
        [Test]
        public void Should_next_player_be_who_played_bomb_when_not_finished()
        {

            var trickPlay = new TrickPlay(3);
            trickPlay.AddAction(HaggisAction.FromTrick("4BYOG_QUAD", Piotr));

            //Play Bomb
            trickPlay.AddAction(
                HaggisAction.FromTrick(new Trick(Haggis.Domain.Enums.TrickType.BOMB, new string[] { "3Y", "5O", "7R", "9B" }.ToCards()), Slawek));

            trickPlay.AddAction(HaggisAction.Pass(Robert));
            Assert.That(trickPlay.IsEndingPass(), Is.False);

            trickPlay.AddAction(HaggisAction.Pass(Piotr));
            Assert.That(trickPlay.IsEndingPass(), Is.True);
            Assert.That(trickPlay.Taking() == Piotr, Is.True);


        }

        [Test]
        public void Should_next_player_be_who_played_2_bomb_when_not_finished()
        {
            var trickPlay = new TrickPlay(3);
            trickPlay.AddAction(HaggisAction.FromTrick("4BYOG_QUAD", Piotr));

            //Play Bomb
            trickPlay.AddAction(
                HaggisAction.FromTrick(new Trick(Haggis.Domain.Enums.TrickType.BOMB, new string[] { "3Y", "5O", "7R", "9B" }.ToCards()), Slawek));

            trickPlay.AddAction(
                HaggisAction.FromTrick(new Trick(Haggis.Domain.Enums.TrickType.BOMB, new string[] { "3O", "5O", "7O", "9O" }.ToCards()), Robert));
            Assert.That(trickPlay.IsEndingPass(), Is.False);

            trickPlay.AddAction(HaggisAction.Pass(Piotr));
            Assert.That(trickPlay.IsEndingPass(), Is.False);

            trickPlay.AddAction(HaggisAction.Pass(Slawek));
            Assert.That(trickPlay.IsEndingPass(), Is.True);
            Assert.That(trickPlay.Taking() == Slawek, Is.True);

        }
        [Test]
        public void Should_next_player_played_bomb_first()
        {

            var trickPlay = new TrickPlay(3);
            trickPlay.AddAction(HaggisAction.FromTrick(new Trick(Haggis.Domain.Enums.TrickType.BOMB, new string[] { "3Y", "5O", "7R", "9B" }.ToCards()), Piotr)
                );

            trickPlay.AddAction(HaggisAction.Pass(Slawek));
            Assert.That(trickPlay.IsEndingPass(), Is.False);


            trickPlay.AddAction(HaggisAction.Pass(Robert));
            Assert.That(trickPlay.IsEndingPass(), Is.True);

            Assert.That(trickPlay.Taking() == Piotr, Is.True);

        }

        [Test]
        public void Should_next_player_be_last_of_best_trick_played_scenario_when_not_finished()
        {
            var trickPlay = new TrickPlay(3);
            trickPlay.AddAction(HaggisAction.FromTrick("4BYOG_QUAD", Piotr));
            trickPlay.AddAction(HaggisAction.Pass(Slawek));

            Assert.That(trickPlay.IsEndingPass(), Is.False);

            trickPlay.AddAction(HaggisAction.Pass(Robert));
            Assert.That(trickPlay.IsEndingPass(), Is.True);

            Assert.That(trickPlay.Taking() == Piotr, Is.True);
        }

        [Test]
        public void Should_Clone_TrickPlay()
        {
            var trickPlay = new TrickPlay(3);
            trickPlay.AddAction(HaggisAction.FromTrick("4BYOG_QUAD", Piotr));
            trickPlay.AddAction(HaggisAction.Pass(Slawek));

            var clonedTrickPlay = (TrickPlay)trickPlay.Clone();

            Assert.That(clonedTrickPlay != trickPlay);

            Assert.That(clonedTrickPlay.Actions, Is.Not.Null);
        }

        [Test]
        public void ShouldReduceNumberOfPlayersAfterPlayerFinishesActionPass()
        {

            Piotr = new HaggisPlayer("Piotr");
            Slawek = new HaggisPlayer("S³awek");
            Robert = new HaggisPlayer("Robert");

            Piotr.Hand = new List<Card>();
            Slawek.Hand = new List<String> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<String> { "2O", "2B" }.ToCards();

            var trickPlay = new TrickPlay(3);

            Trick finalTrick = "2G_SINGLE".ToTrick();
            finalTrick.IsFinal = true;
            trickPlay.AddAction(HaggisAction.FromTrick(finalTrick, Piotr));
            Assert.That(trickPlay.NumberOfPlayers.Equals(3), Is.True);

            trickPlay.AddAction(HaggisAction.FromTrick("4G_SINGLE", Slawek));
            trickPlay.AddAction(HaggisAction.Pass(Robert));

            Assert.That(trickPlay.IsEndingPass(), Is.True);
            Assert.That(trickPlay.Taking() == Slawek, Is.True);

        }

        [Test]
        public void ShouldReduceNumberOfPlayersAfterPlayerFinishesPassAction()
        {

            Piotr = new HaggisPlayer("Piotr");
            Slawek = new HaggisPlayer("S³awek");
            Robert = new HaggisPlayer("Robert");

            Piotr.Hand = new List<Card>();
            Slawek.Hand = new List<String> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<String> { "3O", "2B" }.ToCards();

            var trickPlay = new TrickPlay(3);

            Trick finalTrick = "2G_SINGLE".ToTrick();
            finalTrick.IsFinal = true;
            trickPlay.AddAction(HaggisAction.FromTrick(finalTrick, Piotr));
            
            trickPlay.AddAction(HaggisAction.Pass(Slawek));
            Assert.That(trickPlay.IsEndingPass(), Is.False);

            trickPlay.AddAction(HaggisAction.FromTrick("3O_SINGLE", Robert));
            Assert.That(trickPlay.IsEndingPass(), Is.False);

            Assert.That(trickPlay.Taking() == Robert, Is.True);

        } 
        
        [Test]
        public void ShouldReduceNumberOfPlayersAfterPlayerFinishesPassPass()
        {

            Piotr = new HaggisPlayer("Piotr");
            Slawek = new HaggisPlayer("S³awek");
            Robert = new HaggisPlayer("Robert");

            Piotr.Hand = new List<Card>();
            Slawek.Hand = new List<String> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<String> { "3O", "2B" }.ToCards();

            var trickPlay = new TrickPlay(3);

            Trick finalTrick = "2G_SINGLE".ToTrick();
            finalTrick.IsFinal = true;
            trickPlay.AddAction(HaggisAction.FromTrick(finalTrick, Piotr));
            
            trickPlay.AddAction(HaggisAction.Pass(Slawek));
            Assert.That(trickPlay.IsEndingPass(), Is.False);

            trickPlay.AddAction(HaggisAction.Pass(Robert));
            Assert.That(trickPlay.IsEndingPass(), Is.True);
 
            Assert.That(trickPlay.Taking() == Piotr, Is.True);
        }
        
        [Test]
        public void ShouldReduceNumberOfPlayersAfterPlayerFinishesTwice()
        {

            Piotr = new HaggisPlayer("Piotr");
            Slawek = new HaggisPlayer("S³awek");
            Robert = new HaggisPlayer("Robert");

            Piotr.Hand = new List<Card>();
            Slawek.Hand = new List<String> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<String> { "3O", "2B" }.ToCards();

            var trickPlay = new TrickPlay(3);

            Trick finalTrick = "2G_SINGLE".ToTrick();
            finalTrick.IsFinal = true;
            trickPlay.AddAction(HaggisAction.FromTrick(finalTrick, Piotr));
            

            Trick finalTrick2 = "4G_SINGLE".ToTrick();
            finalTrick2.IsFinal = true;
            trickPlay.AddAction(HaggisAction.FromTrick(finalTrick2, Slawek));

            Assert.That(trickPlay.Taking() == Slawek, Is.True);
        }
    }
    
}
