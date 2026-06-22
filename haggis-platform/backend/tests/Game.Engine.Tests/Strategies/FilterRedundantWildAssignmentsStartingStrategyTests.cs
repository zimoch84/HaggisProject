using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
using Haggis.AI.Model;
using Haggis.AI.StartingTrickFilterStrategies;
using Haggis.AI.Strategies;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests.Strategies
{
    [TestFixture]
    internal sealed class FilterRedundantWildAssignmentsStartingStrategyTests
    {
        [Test]
        public void FilterTricks_WhenSameTypeHasDifferentWildCounts_ShouldKeepLowestWildUsage()
        {
            var strategy = new FilterRedundantWildAssignmentsStartingStrategy();
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

            var filtered = strategy.FilterTricks(new List<Trick> { twoWilds, oneWild });

            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered.Single(), Is.SameAs(oneWild));
        }

        [Test]
        public void FilterTricks_WhenSameTypeHasSameWildCount_ShouldKeepLowestWildRanks()
        {
            var strategy = new FilterRedundantWildAssignmentsStartingStrategy();
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

            var filtered = strategy.FilterTricks(new List<Trick> { withQueen, withJack });

            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered.Single(), Is.SameAs(withJack));
        }

        [Test]
        public void FilterTricks_WhenNaturalEquivalentExists_ShouldKeepNaturalsAndRemoveWildcardVariant()
        {
            var strategy = new FilterRedundantWildAssignmentsStartingStrategy();
            var naturalPair10A = "10RB_PAIR".ToTrick();
            var naturalPair10B = "10RG_PAIR".ToTrick();
            var wildPair10 = new Trick(TrickType.PAIR, new List<Card>
            {
                "10B".ToCard(),
                "J".ToCard().WildAs("10G".ToCard())
            });

            var filtered = strategy.FilterTricks(new List<Trick> { naturalPair10A, naturalPair10B, wildPair10 });

            Assert.That(filtered, Has.Count.EqualTo(2));
            Assert.That(filtered, Does.Contain(naturalPair10A));
            Assert.That(filtered, Does.Contain(naturalPair10B));
            Assert.That(filtered, Does.Not.Contain(wildPair10));
        }

        [Test]
        public void FilterTricks_WhenNaturalTrickHasDifferentEffectiveRank_ShouldNotRemoveWildcardAlternative()
        {
            var strategy = new FilterRedundantWildAssignmentsStartingStrategy();
            var naturalPair9 = "9RB_PAIR".ToTrick();
            var wildcardPair10 = new Trick(TrickType.PAIR, new List<Card>
            {
                "10B".ToCard(),
                "J".ToCard().WildAs("10G".ToCard())
            });

            var filtered = strategy.FilterTricks(new List<Trick> { naturalPair9, wildcardPair10 });

            Assert.That(filtered, Has.Count.EqualTo(2));
            Assert.That(filtered, Does.Contain(naturalPair9));
            Assert.That(filtered, Does.Contain(wildcardPair10));
        }

        [Test]
        public void StartingTrickStrategy_WhenNoOptionalFilterIsConfigured_ShouldKeepRedundantWildAssignments()
        {
            var diagnostics = CaptureDiagnostics(new StartingTrickStrategy(new FilterNoneStrategy(), new ZeroWeightStartingStrategy()));

            Assert.That(diagnostics.Count, Is.GreaterThan(CaptureDiagnostics(
                new StartingTrickStrategy(
                    new FilterRedundantWildAssignmentsStartingStrategy(),
                    new ZeroWeightStartingStrategy())).Count));
        }

        [Test]
        public void StartingTrickStrategy_WhenOptionalFilterIsConfigured_ShouldRemoveRedundantWildAssignments()
        {
            var unfilteredDiagnostics = CaptureDiagnostics(new StartingTrickStrategy(new FilterNoneStrategy(), new ZeroWeightStartingStrategy()));
            var filteredDiagnostics = CaptureDiagnostics(
                new StartingTrickStrategy(
                    new FilterRedundantWildAssignmentsStartingStrategy(),
                    new ZeroWeightStartingStrategy()));

            Assert.That(filteredDiagnostics.Count, Is.LessThan(unfilteredDiagnostics.Count));
        }

        private static List<string> CaptureDiagnostics(StartingTrickStrategy strategy)
        {
            var diagnostics = new List<string>();
            var previousDiagnosticsSink = StartingTrickStrategy.DiagnosticsSink;

            try
            {
                StartingTrickStrategy.DiagnosticsSink = diagnostics.Add;
                var p1 = new AIPlayer("p1", strategy)
                {
                    Hand = new List<string> { "10B", "J", "Q", "3R" }.ToCards()
                };
                var p2 = new AIPlayer("p2", HeuristicPlayStrategy.Create())
                {
                    Hand = new List<string> { "4R" }.ToCards()
                };
                var p3 = new AIPlayer("p3", HeuristicPlayStrategy.Create())
                {
                    Hand = new List<string> { "5R" }.ToCards()
                };
                var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

                _ = p1.GetPlayingAction(state);
                return diagnostics;
            }
            finally
            {
                StartingTrickStrategy.DiagnosticsSink = previousDiagnosticsSink;
            }
        }

        private sealed class ZeroWeightStartingStrategy : IStartingTrickWeightStrategy
        {
            public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
            {
                return (allSuggestedTricks ?? new List<Trick>())
                    .Select(trick => (0, trick))
                    .ToList();
            }
        }
    }
}
