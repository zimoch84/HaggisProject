using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
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
    internal sealed class PreferTricksWithMoreContinuationsWeightStrategyTests
    {
        [Test]
        public void GetWeight_WhenTrickHasMoreContinuations_ShouldReturnHigherWeight()
        {
            var strategy = new PreferTricksWithMoreContinuationsWeightStrategy(1);
            var lowerPair = new Trick(TrickType.PAIR, new List<Card> { "2R".ToCard(), "2B".ToCard() });
            var higherPair = new Trick(TrickType.PAIR, new List<Card> { "3R".ToCard(), "3B".ToCard() });
            var highestPair = new Trick(TrickType.PAIR, new List<Card> { "4R".ToCard(), "4B".ToCard() });
            var tricks = new List<Trick> { lowerPair, higherPair, highestPair };

            var weights = strategy.GetWeight(tricks, null);
            var lowerWeight = weights.Single(result => ReferenceEquals(result.Trick, lowerPair)).Weight;
            var higherWeight = weights.Single(result => ReferenceEquals(result.Trick, higherPair)).Weight;

            Assert.That(lowerWeight, Is.EqualTo(20));
            Assert.That(higherWeight, Is.EqualTo(10));
        }

        [Test]
        public void GetWeight_WhenTrickIsSingle_ShouldReturnZero()
        {
            var strategy = new PreferTricksWithMoreContinuationsWeightStrategy(1);
            var single = new Trick(TrickType.SINGLE, new List<Card> { "2R".ToCard() });
            var otherSingle = new Trick(TrickType.SINGLE, new List<Card> { "3R".ToCard() });
            var tricks = new List<Trick> { single, otherSingle };

            var weights = strategy.GetWeight(tricks, null);
            var singleWeight = weights.Single(result => ReferenceEquals(result.Trick, single)).Weight;

            Assert.That(singleWeight, Is.Zero);
        }

        [Test]
        public void StartingTrickStrategy_WhenCombinationHasMoreContinuations_ShouldPreferIt()
        {
            var strategy = new StartingTrickStrategy(
                new PairsOnlyFilterStrategy(),
                heuristicOptions: new HeuristicOptions { ContinuationCountWeight = 1 });

            var p1 = new Haggis.AI.Model.AIPlayer("p1", strategy)
            {
                Hand = new List<string> { "2R", "2B", "3R", "3B", "4R", "4B" }.ToCards()
            };
            var p2 = new Haggis.AI.Model.AIPlayer("p2", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "5R" }.ToCards()
            };
            var p3 = new Haggis.AI.Model.AIPlayer("p3", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "6R" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Desc, Is.EqualTo("PAIR[2R|2B]"));
        }

        private sealed class PairsOnlyFilterStrategy : IStartingTrickFilterStrategy
        {
            public List<Trick> FilterTricks(List<Trick> tricks)
            {
                return tricks.Where(trick => trick.Type == TrickType.PAIR).ToList();
            }
        }
    }
}
