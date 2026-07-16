using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Model;
using Haggis.AI.StartingTrickFilterStrategies;
using Haggis.AI.StartingTrickWeightStrategies;
using Haggis.AI.Strategies;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests.Strategies
{
    [TestFixture]
    internal sealed class PreferLowerTricksWhenHandIsLargeWeightStrategyTests
    {
        [Test]
        public void GetWeight_WhenHandIsLarge_ShouldPreferLowerTrick()
        {
            var strategy = new PreferLowerTricksWhenHandIsLargeWeightStrategy(1);
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2R", "3R", "4R", "5R", "6R", "7R", "8R", "9R", "10R", "J", "Q", "K" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });
            var lowSingle = new Trick(TrickType.SINGLE, new List<Card> { "2R".ToCard() });
            var highSingle = new Trick(TrickType.SINGLE, new List<Card> { "10R".ToCard() });
            var weights = strategy.GetWeight(new List<Trick> { lowSingle, highSingle }, state);

            var lowWeight = weights.Single(result => ReferenceEquals(result.Trick, lowSingle)).Weight;
            var highWeight = weights.Single(result => ReferenceEquals(result.Trick, highSingle)).Weight;

            Assert.That(lowWeight, Is.GreaterThan(highWeight));
            Assert.That(lowWeight, Is.InRange(-10, 10));
            Assert.That(highWeight, Is.InRange(-10, 10));
        }

        [Test]
        public void GetWeight_WhenHandIsSmall_ShouldPreferHigherTrick()
        {
            var strategy = new PreferLowerTricksWhenHandIsLargeWeightStrategy(1);
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2R", "10R", "J", "Q" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });
            var lowSingle = new Trick(TrickType.SINGLE, new List<Card> { "2R".ToCard() });
            var highSingle = new Trick(TrickType.SINGLE, new List<Card> { "10R".ToCard() });
            var weights = strategy.GetWeight(new List<Trick> { lowSingle, highSingle }, state);

            var lowWeight = weights.Single(result => ReferenceEquals(result.Trick, lowSingle)).Weight;
            var highWeight = weights.Single(result => ReferenceEquals(result.Trick, highSingle)).Weight;

            Assert.That(highWeight, Is.GreaterThan(lowWeight));
            Assert.That(lowWeight, Is.InRange(-10, 10));
            Assert.That(highWeight, Is.InRange(-10, 10));
        }

        [Test]
        public void StartingTrickStrategy_WhenHandIsLarge_ShouldPreferLowerOpening()
        {
            var strategy = new StartingTrickStrategy(
                new FilterNoneStrategy(),
                startingTrickWeightStrategies: new[]
                {
                    new PreferLowerTricksWhenHandIsLargeWeightStrategy(1)
                });

            var p1 = new AIPlayer("p1", strategy)
            {
                Hand = new List<string> { "2R", "5R", "7R", "8R", "9R", "10R", "3B", "4B", "5B" }.ToCards()
            };
            var p2 = new AIPlayer("p2", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "J" }.ToCards()
            };
            var p3 = new AIPlayer("p3", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "Q" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Desc, Is.EqualTo("SINGLE[2R]"));
        }
    }
}
