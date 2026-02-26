using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static Haggis.Domain.Extentions.CardsExtensions;
using static Haggis.Domain.Model.HaggisAction;

namespace HaggisTests
{
    internal class MoveGenerationServiceTests
    {
        private HaggisPlayer Piotr;
        private HaggisPlayer Slawek;
        private HaggisPlayer Robert;
        private HaggisGameState GameState;
        private MoveGenerationService Service;

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
            Service = new MoveGenerationService();
        }

        [Test]
        public void GetPossibleActionsForCurrentPlayer_ShouldReturnPlayableActionsAndPass_OnFreshTrick()
        {
            var actions = Service.GetPossibleActionsForCurrentPlayer(GameState);

            Assert.That(actions.Contains(Pass(Piotr)), Is.True);
            Assert.That(actions.Contains(FromTrick("2Y_SINGLE", Piotr)), Is.True);
            Assert.That(actions.Contains(FromTrick("3Y_SINGLE", Piotr)), Is.True);
        }

        [Test]
        public void GetPossibleActionsForCurrentPlayer_ShouldIncludePassAndComparableTrick_AfterOpeningPlay()
        {
            GameState.ApplyAction(FromTrick("3Y_SINGLE", Piotr));

            var actions = Service.GetPossibleActionsForCurrentPlayer(GameState);

            Assert.That(GameState.CurrentPlayer, Is.EqualTo(Slawek));
            Assert.That(actions.Contains(Pass(Slawek)), Is.True);
            Assert.That(actions.Contains(FromTrick("4G_SINGLE", Slawek)), Is.True);
            Assert.That(actions.Any(action => !action.IsPass && action.Trick.ToString() == "2G_SINGLE"), Is.False);
        }

        [Test]
        public void GetPossibleActionsForCurrentPlayer_ShouldReturnEmpty_WhenRoundIsOver()
        {
            Piotr.Hand = new List<Card>();
            Slawek.Hand = new List<Card>();

            var actions = Service.GetPossibleActionsForCurrentPlayer(GameState);

            Assert.That(GameState.RoundOver(), Is.True);
            Assert.That(actions, Is.Empty);
        }
    }
}
