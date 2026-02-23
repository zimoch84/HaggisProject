using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static Haggis.Domain.Extentions.CardsExtensions;
using static Haggis.Domain.Model.HaggisAction;

namespace HaggisTests
{
    internal class HaggisGameStateTests
    {
        HaggisPlayer Piotr;
        HaggisPlayer Slawek;
        HaggisPlayer Robert;

        HaggisGameState GameState;

        IList<HaggisAction> Actions => GameState.Actions;

        [SetUp]
        public void SetUp()
        {

            Piotr = new HaggisPlayer("Piotr");
            Slawek = new HaggisPlayer("S³awek");
            Robert = new HaggisPlayer("Robert");

            Piotr.Hand = Cards("2Y", "3Y");
            Slawek.Hand = Cards("2G", "4G");
            Robert.Hand = Cards("2O", "2B");

            GameState = new HaggisGameState(new List<IHaggisPlayer> { Piotr, Slawek, Robert });

        }
        [Test]
        public void ApplyActionToBoard_WhenFirstAction()
        {

            GameState.ApplyActionToBoard(FromTrick("2Y_SINGLE", Piotr));
            Assert.That(Piotr.Hand.Count, Is.EqualTo(1));
            Assert.That(Piotr.Hand.Contains("3Y"), Is.True);

        }

        [Test]
        public void HaggisPossibleAction1Test()
        {

            Assert.That(Actions.Contains(Pass(Piotr)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2Y_SINGLE", Piotr)), Is.True);
            Assert.That(Actions.Contains(FromTrick("3Y_SINGLE", Piotr)), Is.True);

            GameState.SetCurrentPlayer(Slawek);
            Assert.That(Actions.Contains(Pass(Slawek)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2G_SINGLE", Slawek)), Is.True);
            Assert.That(Actions.Contains(FromTrick("4G_SINGLE", Slawek)), Is.True);

            GameState.SetCurrentPlayer(Robert);

            Assert.That(Actions.Contains(Pass(Robert)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2O_SINGLE", Robert)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2B_SINGLE", Robert)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2BO_PAIR", Robert)), Is.True);

        }

        /*
         * 3Y
         * 4G
         * PASS
         * PASS
         * */
        [Test]
        public void HaggisPossibleActionsAfterMove1()
        {

            Assert.That(Actions.Contains(Pass(Piotr)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2Y_SINGLE", Piotr)), Is.True);
            Assert.That(Actions.Contains(FromTrick("3Y_SINGLE", Piotr)), Is.True);
            GameState.ApplyAction(FromTrick("3Y_SINGLE", Piotr));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(Actions.Count, Is.EqualTo(2));
            Assert.That(Actions.Contains(Pass(Slawek)), Is.True);
            Assert.That(Actions.Contains(FromTrick("4G_SINGLE", Slawek)), Is.True);
            GameState.ApplyAction(FromTrick("4G_SINGLE", Slawek));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(Actions.Count, Is.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Robert)), Is.True);
            GameState.ApplyAction(Pass(Robert));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(Actions.Count, Is.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Piotr)), Is.True);
            GameState.ApplyAction(Pass(Piotr));

            //TrickPlay 1 is over
            Assert.That(GameState.ActionArchive.Count, Is.EqualTo(1));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(Actions.Count, Is.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Slawek)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2G_SINGLE", Slawek)), Is.True);
            Assert.That(Actions.First().Trick.IsFinal, Is.True);
            GameState.ApplyAction(FromTrick("2G_SINGLE", Slawek));

            Assert.That(Slawek.Finished, Is.True);
            Assert.That(GameState.RoundOver(), Is.False);
            Assert.That(Slawek.Score, Is.EqualTo(15));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(Actions.Count, Is.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Robert)), Is.True);
            GameState.ApplyAction(Pass(Robert));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(Actions.Count, Is.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Piotr)), Is.True);
            GameState.ApplyAction(Pass(Piotr));

            //TrickPlay 2 is over
            Assert.That(GameState.ActionArchive.Count, Is.EqualTo(2));


            Assert.That(Slawek.Finished, Is.True);

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(Actions.Count, Is.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Robert)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2O_SINGLE", Robert)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2B_SINGLE", Robert)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2BO_PAIR", Robert)), Is.True);
            Assert.That(Actions.First().Trick.IsFinal, Is.True);
            GameState.ApplyAction(FromTrick("2BO_PAIR", Robert));

            Assert.That(Robert.Finished, Is.True);
            Assert.That(Robert.Score, Is.EqualTo(5));
            Assert.That(Slawek.Score, Is.EqualTo(16));
            Assert.That(Slawek.Score, Is.EqualTo(16));
            Assert.That(Piotr.Score, Is.EqualTo(0));

            Assert.That(GameState.RoundOver(), Is.True);
        }

        /*
         * 2Y
         * 4G
         * PASS
         * PASS
         * */
        [Test]
        public void HaggisPossibleActionsAfterMove2()
        {

            Assert.That(Actions.Contains(Pass(Piotr)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2Y_SINGLE", Piotr)), Is.True);
            Assert.That(Actions.Contains(FromTrick("3Y_SINGLE", Piotr)), Is.True);
            GameState.ApplyAction(FromTrick("2Y_SINGLE", Piotr));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(Actions, Has.Count.EqualTo(2));
            Assert.That(Actions.Contains(Pass(Slawek)), Is.True);
            Assert.That(Actions.Contains(FromTrick("4G_SINGLE", Slawek)), Is.True);
            GameState.ApplyAction(FromTrick("4G_SINGLE", Slawek));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(Actions, Has.Count.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Robert)), Is.True);
            GameState.ApplyAction(Pass(Robert));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(Actions, Has.Count.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Piotr)), Is.True);
            GameState.ApplyAction(Pass(Piotr));

            //TrickPlay 1 is over
            Assert.That(GameState.ActionArchive, Has.Count.EqualTo(1));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(Actions, Has.Count.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Slawek)), Is.False);
            Assert.That(Actions.Contains(FromTrick("2G_SINGLE", Slawek)), Is.True);
            Assert.That(Actions.First().Trick.IsFinal, Is.True);
            GameState.ApplyAction(FromTrick("2G_SINGLE", Slawek));

            Assert.That(Slawek.Finished, Is.True);
            Assert.That(GameState.RoundOver(), Is.False);
            Assert.That(Slawek.Score, Is.EqualTo(15));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(Actions, Has.Count.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Robert)), Is.True);
            GameState.ApplyAction(Pass(Robert));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(Actions, Has.Count.EqualTo(1));
            Assert.That(Actions.Contains(Pass(Piotr)), Is.False);
            Assert.That(Actions.Contains(FromTrick("3Y_SINGLE", Piotr)), Is.True);
            Assert.That(Actions.First().Trick.IsFinal, Is.True);
            GameState.ApplyAction(FromTrick("3Y_SINGLE", Piotr));

            //TrickPlay 2 is over
            Assert.That(GameState.ActionArchive, Has.Count.EqualTo(2));

            Assert.That(Slawek.Finished, Is.True);
            Assert.That(Piotr.Finished, Is.True);

            Assert.That(Robert.Score, Is.EqualTo(0));
            Assert.That(Slawek.Score, Is.EqualTo(15));
            Assert.That(Piotr.Score, Is.EqualTo(10));

            Assert.That(GameState.RoundOver(), Is.True);
        }

        [Test]
        public void Should_next_player_be_who_played_bomb_when_not_finished()
        {
            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Slawek));
            GameState.ApplyAction(FromTrick("4BYOG_QUAD", Piotr));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Robert));
            //Play Bomb
            GameState.ApplyAction(
                FromTrick(new Trick(Haggis.Domain.Enums.TrickType.BOMB, CardsExtensions.Cards("3Y", "5O", "7R", "9B")), Slawek));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Piotr));
            GameState.ApplyAction(Pass(Robert));


            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Slawek));
            GameState.ApplyAction(Pass(Piotr));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Robert));

        }

        [Test]
        public void Should_next_player_be_who_played_2_bomb_when_not_finished()
        {
            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Slawek));

            GameState.ApplyAction(FromTrick("4BYOG_QUAD", Piotr));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Robert));
            //Play Bomb
            GameState.ApplyAction(
                FromTrick(new Trick(Haggis.Domain.Enums.TrickType.BOMB, Cards("3Y", "5O", "7R", "9B")), Slawek));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Piotr));
            GameState.ApplyAction(
                FromTrick(new Trick(Haggis.Domain.Enums.TrickType.BOMB, Cards("3O", "5O", "7O", "9O")), Robert));


            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Slawek));
            GameState.ApplyAction(Pass(Piotr));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Robert));
            GameState.ApplyAction(Pass(Slawek));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Piotr));


        }
        [Test]
        public void Should_next_player_played_bomb_first()
        {
            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Slawek));
            GameState.ApplyAction(FromTrick(new Trick(Haggis.Domain.Enums.TrickType.BOMB, Cards("3Y", "5O", "7R", "9B")), Piotr)
                );

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Robert));
            GameState.ApplyAction(Pass(Slawek));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Piotr));
            GameState.ApplyAction(Pass(Robert));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Slawek));

        }

        [Test]
        public void Should_next_player_be_last_of_best_trick_played_scenario_when_not_finished()
        {
            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Slawek));

            GameState.ApplyAction(FromTrick("4BYOG_QUAD", Piotr));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Robert));
            GameState.ApplyAction(Pass(Slawek));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Robert));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Piotr));
            GameState.ApplyAction(Pass(Robert));

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Piotr));
            Assert.That(GameState.NextPlayer, Is.EqualTo(Slawek));

        }

        [Test]
        public void Clone_ShouldClonedInsanceDoesntAffectCopiedOne()
        {
            var avalaibleActions = GameState.Actions;
            var gameStateClone = (HaggisGameState)GameState.Clone();
            var avalaibleClonedActions = gameStateClone.Actions;

            Assert.That(avalaibleClonedActions, Is.EqualTo(avalaibleActions));

            Assert.That(GameState.Players[0], Is.Not.SameAs(gameStateClone.Players[0]));
            Assert.That(GameState.Actions, Is.Not.SameAs(gameStateClone.Actions));

            Assert.That(GameState.Actions.Count, Is.EqualTo(2));

            gameStateClone.ApplyAction(gameStateClone.Actions[0]);

            Assert.That(GameState.Actions.Count, Is.EqualTo(2));
        }

        [Test]
        public void Clone_ShouldCloneTrickPlayHistory()
        {
            while (GameState.Actions.Count > 0)
            {
                GameState.ApplyAction(GameState.Actions[0]);
            }
            var history = GameState.ActionArchive;
            var gameStateClone = (HaggisGameState)GameState.Clone();
            var clonedHistory = gameStateClone.ActionArchive;

            var historyEnumerator = history.GetEnumerator();
            var clonedHistoryEnumerator = clonedHistory.GetEnumerator();

            while (historyEnumerator.MoveNext() && clonedHistoryEnumerator.MoveNext())
            {
                Assert.That(historyEnumerator.Current, Is.EqualTo(clonedHistoryEnumerator.Current));
            }
        }
    }
}
