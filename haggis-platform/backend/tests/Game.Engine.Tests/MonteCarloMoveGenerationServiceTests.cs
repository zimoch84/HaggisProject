using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using MonteCarlo;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static Haggis.Domain.Extentions.CardsExtensions;
using static Haggis.Domain.Model.HaggisAction;

namespace HaggisTests
{
    internal class MonteCarloMoveGenerationServiceTests
    {
        private sealed class KeepHighestSingleTrickStrategy : IMonteCarloTrickSelectionStrategy
        {
            public IList<Trick> Select(HaggisGameState state, IList<Trick> generatedTricks, bool isOpeningTrick)
            {
                var highestSingle = generatedTricks
                    .Where(t => t.Type == Haggis.Domain.Enums.TrickType.SINGLE)
                    .OrderByDescending(t => t.FirstCard().Rank)
                    .First();

                return new List<Trick> { highestSingle };
            }
        }

        [Test]
        public void GetPossibleActionsForCurrentPlayer_ShouldPreferFinalAction_AndNotAddPass_OnFreshTrick()
        {
            var piotr = new HaggisPlayer("Piotr") { Hand = Cards("3Y") };
            var slawek = new HaggisPlayer("Sławek") { Hand = Cards("2G", "4G") };
            var robert = new HaggisPlayer("Robert") { Hand = Cards("2O", "2B") };
            var state = new HaggisGameState(new List<IHaggisPlayer> { piotr, slawek, robert });
            var service = new MonteCarloMoveGenerationService();

            var actions = service.GetPossibleActionsForCurrentPlayer(state);

            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions[0].IsPass, Is.False);
            Assert.That(actions[0].IsFinal, Is.True);
            Assert.That(actions[0], Is.EqualTo(FromTrick("3Y_SINGLE", piotr)));
        }

        [Test]
        public void GetPossibleActionsForCurrentPlayer_ShouldAddPass_WhenNoFinalActionAndTrickAlreadyStarted()
        {
            var piotr = new HaggisPlayer("Piotr") { Hand = Cards("2Y", "3Y") };
            var slawek = new HaggisPlayer("Sławek") { Hand = Cards("2G", "4G") };
            var robert = new HaggisPlayer("Robert") { Hand = Cards("2O", "2B") };
            var state = new HaggisGameState(new List<IHaggisPlayer> { piotr, slawek, robert });
            state.ApplyAction(FromTrick("3Y_SINGLE", piotr));

            var service = new MonteCarloMoveGenerationService(new SelectAllMonteCarloActionsStrategy());
            var actions = service.GetPossibleActionsForCurrentPlayer(state);

            Assert.That(state.CurrentPlayer, Is.EqualTo(slawek));
            Assert.That(actions.Contains(FromTrick("4G_SINGLE", slawek)), Is.True);
            Assert.That(actions.Contains(Pass(slawek)), Is.True);
        }

        [Test]
        public void GetPossibleActionsForCurrentPlayer_ShouldNotAddPass_WhenFinalActionExists()
        {
            var piotr = new HaggisPlayer("Piotr") { Hand = Cards("2Y", "3Y") };
            var slawek = new HaggisPlayer("Sławek") { Hand = Cards("4G") };
            var robert = new HaggisPlayer("Robert") { Hand = Cards("2O", "2B") };
            var state = new HaggisGameState(new List<IHaggisPlayer> { piotr, slawek, robert });
            state.ApplyAction(FromTrick("2Y_SINGLE", piotr));

            var service = new MonteCarloMoveGenerationService();
            var actions = service.GetPossibleActionsForCurrentPlayer(state);

            Assert.That(state.CurrentPlayer, Is.EqualTo(slawek));
            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions.All(a => !a.IsPass), Is.True);
            Assert.That(actions[0].IsFinal, Is.True);
            Assert.That(actions[0], Is.EqualTo(FromTrick("4G_SINGLE", slawek)));
        }

        [Test]
        public void GetPossibleActionsForCurrentPlayer_ShouldUseTrickSelectionStrategy_BeforeActionSelection()
        {
            var piotr = new HaggisPlayer("Piotr") { Hand = Cards("2Y", "3Y") };
            var slawek = new HaggisPlayer("Sławek") { Hand = Cards("2G", "4G") };
            var robert = new HaggisPlayer("Robert") { Hand = Cards("2O", "2B") };
            var state = new HaggisGameState(new List<IHaggisPlayer> { piotr, slawek, robert });

            var service = new MonteCarloMoveGenerationService(
                new SelectAllMonteCarloActionsStrategy(),
                new KeepHighestSingleTrickStrategy());

            var actions = service.GetPossibleActionsForCurrentPlayer(state);

            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions[0], Is.EqualTo(FromTrick("3Y_SINGLE", piotr)));
        }
    }
}
