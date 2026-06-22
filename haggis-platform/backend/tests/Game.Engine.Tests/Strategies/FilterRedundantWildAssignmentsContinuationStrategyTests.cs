using System.Collections.Generic;
using System.Linq;
using Haggis.AI.ContinuationTrickFilterStrategies;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests.Strategies
{
    [TestFixture]
    internal sealed class FilterRedundantWildAssignmentsContinuationStrategyTests
    {
        [Test]
        public void FilterTricks_WhenSameTypeHasDifferentWildCounts_ShouldKeepLowestWildUsage()
        {
            var strategy = new FilterRedundantWildAssignmentsContinuationStrategy();
            var oneWild = new Trick(TrickType.QUAD, new List<Card>
            {
                "10B".ToCard(),
                "10G".ToCard(),
                "10O".ToCard(),
                "J".ToCard().WildAs("10Y".ToCard())
            });
            var twoWilds = new Trick(TrickType.QUAD, new List<Card>
            {
                "10B".ToCard(),
                "10G".ToCard(),
                "J".ToCard().WildAs("10O".ToCard()),
                "Q".ToCard().WildAs("10Y".ToCard())
            });

            var filtered = strategy.FilterTricks(new List<Trick> { twoWilds, oneWild }, gameState: null);

            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered.Single(), Is.SameAs(oneWild));
        }

        [Test]
        public void FilterTricks_WhenSameTypeHasSameWildCount_ShouldKeepLowestWildRanks()
        {
            var strategy = new FilterRedundantWildAssignmentsContinuationStrategy();
            var withJack = new Trick(TrickType.QUAD, new List<Card>
            {
                "10B".ToCard(),
                "10G".ToCard(),
                "10O".ToCard(),
                "J".ToCard().WildAs("10Y".ToCard())
            });
            var withQueen = new Trick(TrickType.QUAD, new List<Card>
            {
                "10B".ToCard(),
                "10G".ToCard(),
                "10O".ToCard(),
                "Q".ToCard().WildAs("10Y".ToCard())
            });
            var withKing = new Trick(TrickType.QUAD, new List<Card>
            {
                "10B".ToCard(),
                "10G".ToCard(),
                "10O".ToCard(),
                "K".ToCard().WildAs("10Y".ToCard())
            });

            var filtered = strategy.FilterTricks(new List<Trick> { withKing, withQueen, withJack }, gameState: null);

            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered.Single(), Is.SameAs(withJack));
        }

        [Test]
        public void FilterTricks_WhenSameTypeHasNaturalTricks_ShouldKeepAllNaturals()
        {
            var strategy = new FilterRedundantWildAssignmentsContinuationStrategy();
            var naturalPair9 = "9RB_PAIR".ToTrick();
            var naturalPair10 = "10RB_PAIR".ToTrick();
            var wildPair = new Trick(TrickType.PAIR, new List<Card>
            {
                "10B".ToCard(),
                "J".ToCard().WildAs("10G".ToCard())
            });

            var filtered = strategy.FilterTricks(new List<Trick> { naturalPair9, naturalPair10, wildPair }, gameState: null);

            Assert.That(filtered, Has.Count.EqualTo(2));
            Assert.That(filtered, Does.Contain(naturalPair9));
            Assert.That(filtered, Does.Contain(naturalPair10));
            Assert.That(filtered, Does.Not.Contain(wildPair));
        }

        [Test]
        public void FilterTricks_WhenSameTypeHasDifferentNaturalContinuations_ShouldNotFilterThemOut()
        {
            var strategy = new FilterRedundantWildAssignmentsContinuationStrategy();
            var lowerSequence = "7B_SEQ3".ToTrick();
            var higherSequence = "8B_SEQ3".ToTrick();

            var filtered = strategy.FilterTricks(new List<Trick> { lowerSequence, higherSequence }, gameState: null);

            Assert.That(filtered, Has.Count.EqualTo(2));
            Assert.That(filtered, Does.Contain(lowerSequence));
            Assert.That(filtered, Does.Contain(higherSequence));
        }

        [Test]
        public void FilterTricks_WhenSameTypeHasDifferentWildcardContinuations_ShouldNotTreatThemAsRedundant()
        {
            var strategy = new FilterRedundantWildAssignmentsContinuationStrategy();
            var lowerSequenceWithWild = new Trick(TrickType.SEQ3, new List<Card>
            {
                "J".ToCard().WildAs("7B".ToCard()),
                "8B".ToCard(),
                "9B".ToCard()
            });
            var higherSequenceWithWild = new Trick(TrickType.SEQ3, new List<Card>
            {
                "8B".ToCard(),
                "9B".ToCard(),
                "J".ToCard().WildAs("10B".ToCard())
            });

            var filtered = strategy.FilterTricks(
                new List<Trick> { lowerSequenceWithWild, higherSequenceWithWild },
                gameState: null);

            Assert.That(filtered, Has.Count.EqualTo(2));
            Assert.That(filtered, Does.Contain(lowerSequenceWithWild));
            Assert.That(filtered, Does.Contain(higherSequenceWithWild));
        }

        [Test]
        public void FilterTricks_WhenNaturalTrickHasDifferentEffectiveRank_ShouldNotRemoveWildcardAlternative()
        {
            var strategy = new FilterRedundantWildAssignmentsContinuationStrategy();
            var naturalPair9 = "9RB_PAIR".ToTrick();
            var wildcardPair10 = new Trick(TrickType.PAIR, new List<Card>
            {
                "10B".ToCard(),
                "J".ToCard().WildAs("10G".ToCard())
            });

            var filtered = strategy.FilterTricks(new List<Trick> { naturalPair9, wildcardPair10 }, gameState: null);

            Assert.That(filtered, Has.Count.EqualTo(2));
            Assert.That(filtered, Does.Contain(naturalPair9));
            Assert.That(filtered, Does.Contain(wildcardPair10));
        }
    }
}
