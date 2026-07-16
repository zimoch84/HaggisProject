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
    internal sealed class PreferShorterTricksWhenHandIsLargeWeightStrategyTests
    {
        [Test]
        public void GetWeight_WhenHandIsLarge_ShouldPreferShorterTrick()
        {
            var strategy = new PreferShorterTricksWhenHandIsLargeWeightStrategy(1);
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2R", "2B", "3R", "3B", "4R", "4B", "5R", "5B", "6R", "6B" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });
            var single = new Trick(TrickType.SINGLE, new List<Card> { "2R".ToCard() });
            var pair = new Trick(TrickType.PAIR, new List<Card> { "2R".ToCard(), "2B".ToCard() });
            var triple = new Trick(TrickType.TRIPLE, new List<Card> { "2R".ToCard(), "2B".ToCard(), "2G".ToCard() });

            var tricks = new List<Trick> { single, pair, triple };
            var weights = strategy.GetWeight(tricks, state);
            var singleWeight = weights.Single(result => ReferenceEquals(result.Trick, single)).Weight;
            var pairWeight = weights.Single(result => ReferenceEquals(result.Trick, pair)).Weight;
            var tripleWeight = weights.Single(result => ReferenceEquals(result.Trick, triple)).Weight;

            Assert.That(singleWeight, Is.GreaterThan(pairWeight));
            Assert.That(pairWeight, Is.GreaterThanOrEqualTo(tripleWeight));
            Assert.That(singleWeight, Is.LessThanOrEqualTo(24));
        }

        [Test]
        public void StartingTrickStrategy_WhenHandIsLarge_ShouldPreferShorterOpeningEvenWithWildCandidate()
        {
            var strategy = new StartingTrickStrategy(
                new FilterNoneStrategy(),
                heuristicOptions: new HeuristicOptions
                {
                    PreferSinglesNotBreakingNonWildCombinationsWeight = 0,
                    ContinuationCountWeight = 0,
                    HigherRelatedCombinationOpeningWeight = 0,
                    PreferShorterStartWeight = 1
                });

            var p1 = new AIPlayer("p1", strategy)
            {
                Hand = new List<string> { "2R", "2B", "3R", "3B", "J" }.ToCards()
            };
            var p2 = new AIPlayer("p2", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "5R" }.ToCards()
            };
            var p3 = new AIPlayer("p3", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "6R" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Desc, Does.StartWith("SINGLE["));
        }
    }
}
