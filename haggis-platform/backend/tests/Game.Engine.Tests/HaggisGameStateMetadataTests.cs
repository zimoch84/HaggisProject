using System.Collections.Generic;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests
{
    [TestFixture]
    public class HaggisGameStateMetadataTests
    {
        [Test]
        public void ApplyAction_ShouldIncreaseMoveIteration()
        {
            var p1 = new HaggisPlayer("P1") { Hand = new List<string> { "2Y", "3Y" }.ToCards() };
            var p2 = new HaggisPlayer("P2") { Hand = new List<string> { "2G", "4G" }.ToCards() };
            var p3 = new HaggisPlayer("P3") { Hand = new List<string> { "2O", "2B" }.ToCards() };

            var state = new HaggisGameState(new List<IHaggisPlayer> { p1, p2, p3 });

            Assert.That(state.MoveIteration, Is.EqualTo(0));
            state.ApplyAction(HaggisAction.FromTrick("2Y_SINGLE", p1));
            Assert.That(state.MoveIteration, Is.EqualTo(1));
        }
    }
}
