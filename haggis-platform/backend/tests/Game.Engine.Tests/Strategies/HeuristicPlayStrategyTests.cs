using System.Collections.Generic;
using Haggis.AI.Model;
using Haggis.AI.Strategies;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests.Strategies
{
    [TestFixture]
    internal sealed class HeuristicPlayStrategyTests
    {
        [Test]
        public void GetPlayingAction_WhenStartingTrickWithLowCards_ShouldPlayLowestSingle()
        {
            var p1 = new AIPlayer("p1", new HeuristicPlayStrategy())
            {
                Hand = new List<string> { "2R", "2B", "2G", "5G", "6R", "6O", "7G" }.ToCards()
            };
            var p2 = new AIPlayer("p2", new HeuristicPlayStrategy())
            {
                Hand = new List<string> { "3R" }.ToCards()
            };
            var p3 = new AIPlayer("p3", new HeuristicPlayStrategy())
            {
                Hand = new List<string> { "4R" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Desc, Is.EqualTo("SINGLE[2R]"));
        }
    }
}
