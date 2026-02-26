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

        IList<HaggisAction> Actions => GameState.PossibleActions;

        [SetUp]
        public void SetUp()
        {

            Piotr = new HaggisPlayer("Piotr");
            Slawek = new HaggisPlayer("Sławek");
            Robert = new HaggisPlayer("Robert");

            Piotr.Hand = Cards("2Y", "3Y");
            Slawek.Hand = Cards("2G", "4G");
            Robert.Hand = Cards("2O", "2B");

            GameState = new HaggisGameState(new List<IHaggisPlayer> { Piotr, Slawek, Robert });

        }
        [Test]
        public void ApplyActionToBoard_WhenFirstAction()
        {

            GameState.ApplyAction(FromTrick("2Y_SINGLE", Piotr));
            Assert.That(Piotr.Hand.Count, Is.EqualTo(1));
            Assert.That(Piotr.Hand.Contains("3Y"), Is.True);

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
            var avalaibleActions = GameState.PossibleActions;
            var gameStateClone = (HaggisGameState)GameState.Clone();
            var avalaibleClonedActions = gameStateClone.PossibleActions;

            Assert.That(avalaibleClonedActions, Is.EqualTo(avalaibleActions));

            Assert.That(GameState.Players[0], Is.Not.SameAs(gameStateClone.Players[0]));
            Assert.That(GameState.PossibleActions, Is.Not.SameAs(gameStateClone.PossibleActions));

            var legalActionsCountBeforeCloneMove = GameState.PossibleActions.Count;

            gameStateClone.ApplyAction(gameStateClone.PossibleActions[0]);

            Assert.That(GameState.PossibleActions.Count, Is.EqualTo(legalActionsCountBeforeCloneMove));
        }

        [Test]
        public void Clone_ShouldCloneTrickPlayHistory()
        {
            while (GameState.PossibleActions.Count > 0)
            {
                GameState.ApplyAction(GameState.PossibleActions[0]);
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

        [Test]
        public void RoundOver_ShouldBeReachable_ByIterativeLegalActionsExecution()
        {
            var maxSteps = 200;
            var step = 0;

            while (!GameState.RoundOver() && step < maxSteps)
            {
                var PossibleActions = GameState.PossibleActions;
                Assert.That(PossibleActions, Is.Not.Empty);

                var chosenAction = PossibleActions.FirstOrDefault(action => !action.IsPass) ?? PossibleActions[0];
                GameState.ApplyAction(chosenAction);
                step++;
            }

            Assert.That(step, Is.LessThan(maxSteps), "Guard reached, round likely did not progress.");
            Assert.That(GameState.RoundOver(), Is.True);
            Assert.That(GameState.Players.Count(player => !player.Finished), Is.EqualTo(1));
            Assert.That(GameState.PossibleActions, Is.Empty);
        }
    }
}




