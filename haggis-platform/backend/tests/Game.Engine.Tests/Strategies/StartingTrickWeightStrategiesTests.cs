using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Interfaces;
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
    internal sealed class StartingTrickWeightStrategiesTests
    {
        [Test]
        public void PreferSinglesNotBreakingNonWildCombinations_WhenSingleDoesNotBreakCombination_ShouldReturnPositiveWeight()
        {
            var strategy = new PreferSinglesNotBreakingNonWildCombinationsWeightStrategy(100);
            var tricks = new List<Trick>
            {
                new Trick(TrickType.SINGLE, new List<Card> { "5G".ToCard() }),
                new Trick(TrickType.PAIR, new List<Card> { "2R".ToCard(), "2B".ToCard() })
            };

            var weight = GetWeight(strategy, tricks[0], tricks, null);

            Assert.That(weight, Is.GreaterThan(0));
        }

        [Test]
        public void PreferSinglesNotBreakingNonWildCombinations_WhenSingleBreaksCombination_ShouldReturnZero()
        {
            var strategy = new PreferSinglesNotBreakingNonWildCombinationsWeightStrategy(100);
            var tricks = new List<Trick>
            {
                new Trick(TrickType.SINGLE, new List<Card> { "2R".ToCard() }),
                new Trick(TrickType.PAIR, new List<Card> { "2R".ToCard(), "2B".ToCard() })
            };

            var weight = GetWeight(strategy, tricks[0], tricks, null);

            Assert.That(weight, Is.Zero);
        }

        [Test]
        public void StartingTrickStrategy_WhenSafeSingleExists_ShouldPreferItOverBreakingCombination()
        {
            var strategy = new StartingTrickStrategy(
                new FilterNoneStrategy(),
                new PreferSinglesNotBreakingNonWildCombinationsWeightStrategy(100));

            var p1 = new AIPlayer("p1", strategy)
            {
                Hand = new List<string> { "2R", "2B", "5G" }.ToCards()
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

            Assert.That(action.Desc, Is.EqualTo("SINGLE[5G]"));
        }

        [Test]
        public void PenalizeWildCardsInOpening_WhenTrickUsesWilds_ShouldSubtractPenaltyPerWildBasedOnHandSize()
        {
            var strategy = new PenalizeWildCardsInOpeningWeightStrategy(10);
            var trick = new Trick(TrickType.QUAD, new List<Card>
            {
                "2R".ToCard(),
                "J".ToCard().WildAs("2R".ToCard()),
                "Q".ToCard().WildAs("2R".ToCard()),
                "K".ToCard().WildAs("2R".ToCard())
            });
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2R", "2B", "2G", "3B", "4B", "5G", "6R", "6B", "6G", "6O", "7G", "9G", "9Y", "10B", "J", "Q", "K" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });

            var weight = GetWeight(strategy, trick, new List<Trick> { trick }, state);

            Assert.That(weight, Is.EqualTo(-(3 * 17 * 10)));
        }

        [Test]
        public void PenalizeWildCardsInOpening_WhenEquivalentTrickExistsWithLowerWild_ShouldDoublePenalty()
        {
            var strategy = new PenalizeWildCardsInOpeningWeightStrategy(10);
            var lowerWildTrick = new Trick(TrickType.PAIR, new List<Card>
            {
                "3B".ToCard(),
                "J".ToCard().WildAs("3B".ToCard())
            });
            var higherWildTrick = new Trick(TrickType.PAIR, new List<Card>
            {
                "3B".ToCard(),
                "K".ToCard().WildAs("3B".ToCard())
            });
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "3B", "J", "Q", "K", "9R" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });

            var weight = GetWeight(strategy, higherWildTrick, new List<Trick> { lowerWildTrick, higherWildTrick }, state);

            Assert.That(weight, Is.EqualTo(-(2 * 5 * 10)));
        }

        [Test]
        public void PenalizeBombOpening_WhenTrickIsBomb_ShouldSubtractPenaltyBasedOnHandSize()
        {
            var strategy = new PenalizeBombOpeningWeightStrategy(100);
            var trick = new Trick(TrickType.BOMB, new List<Card>
            {
                "3O".ToCard(),
                "5Y".ToCard(),
                "7G".ToCard(),
                "9R".ToCard()
            });
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2R", "2B", "2G", "3G", "3O", "4B", "4Y", "4O", "5G", "5Y", "7G", "8G", "9R", "9Y", "J", "Q", "K" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });

            var weight = GetWeight(strategy, trick, new List<Trick> { trick }, state);

            Assert.That(weight, Is.EqualTo(-(17 * 100)));
        }

        [Test]
        public void StartingTrickStrategy_WhenOpeningContainsWildQuad_ShouldPreferNonWildOpening()
        {
            var p1 = new AIPlayer("p1", new HeuristicPlayStrategy())
            {
                Hand = new List<string>
                {
                    "2R", "2B", "2G", "3B", "4B", "5G", "6R", "6B", "6G", "6O", "7G", "9G", "9Y", "10B", "J", "Q", "K"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2", new HeuristicPlayStrategy())
            {
                Hand = new List<string> { "2Y", "2O", "3G", "3O", "4R", "4G", "5O", "6Y", "7R", "7B", "8B", "9O", "10G", "10O", "J", "Q", "K" }.ToCards()
            };
            var p3 = new AIPlayer("p3", new HeuristicPlayStrategy())
            {
                Hand = new List<string> { "3R", "3Y", "4Y", "4O", "5R", "5B", "7O", "8R", "8Y", "8O", "9R", "9B", "10R", "10Y", "J", "Q", "K" }.ToCards()
            };
            var state = new RoundState(
                new List<IHaggisPlayer> { p1, p2, p3 },
                haggisCards: new List<string> { "5Y", "7Y", "8G" }.ToCards());

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Desc, Is.Not.EqualTo("QUAD[2R|J[2]|Q[2]|K[2]]"));
        }

        [Test]
        public void StartingTrickStrategy_WhenUsingBenchmarkOpeningHand_ShouldEmitExpectedBreakdownForTopCandidates()
        {
            var diagnostics = new List<string>();
            var previousDiagnosticsSink = StartingTrickStrategy.DiagnosticsSink;

            try
            {
                StartingTrickStrategy.DiagnosticsSink = diagnostics.Add;

                var p1 = new AIPlayer("p1", new HeuristicPlayStrategy())
                {
                    Hand = new List<string>
                    {
                        "2R", "2B", "2G", "3B", "4B", "5G", "6R", "6B", "6G", "6O", "7G", "9G", "9Y", "10B", "J", "Q", "K"
                    }.ToCards()
                };
                var p2 = new AIPlayer("p2", new HeuristicPlayStrategy())
                {
                    Hand = new List<string> { "2Y", "2O", "3G", "3O", "4R", "4G", "5O", "6Y", "7R", "7B", "8B", "9O", "10G", "10O", "J", "Q", "K" }.ToCards()
                };
                var p3 = new AIPlayer("p3", new HeuristicPlayStrategy())
                {
                    Hand = new List<string> { "3R", "3Y", "4Y", "4O", "5R", "5B", "7O", "8R", "8Y", "8O", "9R", "9B", "10R", "10Y", "J", "Q", "K" }.ToCards()
                };
                var state = new RoundState(
                    new List<IHaggisPlayer> { p1, p2, p3 },
                    haggisCards: new List<string> { "5Y", "7Y", "8G" }.ToCards());

                _ = p1.GetPlayingAction(state);

                Assert.That(diagnostics, Does.Contain(
                    "Starting trick candidate: TRIPLE[2R|2B|2G], weight: 68, breakdown: PreferTricksThatAreMostLikelyNonBreakableWeightStrategy=0, PreferLowerTricksWhenHandIsLargeWeightStrategy=10, PreferShorterTricksWhenHandIsLargeWeightStrategy=18, PenalizeBombOpeningWeightStrategy=0, PenalizeWildCardsInOpeningWeightStrategy=0, PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy=0, PreferTricksWithMoreContinuationsWeightStrategy=40, PreferSinglesNotBreakingNonWildCombinationsWeightStrategy=0"));
                Assert.That(diagnostics, Does.Contain(
                    "Starting trick candidate: SINGLE[10B], weight: 22, breakdown: PreferTricksThatAreMostLikelyNonBreakableWeightStrategy=0, PreferLowerTricksWhenHandIsLargeWeightStrategy=-5, PreferShorterTricksWhenHandIsLargeWeightStrategy=24, PenalizeBombOpeningWeightStrategy=0, PenalizeWildCardsInOpeningWeightStrategy=0, PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy=0, PreferTricksWithMoreContinuationsWeightStrategy=0, PreferSinglesNotBreakingNonWildCombinationsWeightStrategy=3"));
            }
            finally
            {
                StartingTrickStrategy.DiagnosticsSink = previousDiagnosticsSink;
            }
        }

        [Test]
        public void StartingTrickStrategy_WhenOnlyPlayableActionEndsRound_ShouldBypassFilters()
        {
            var strategy = new StartingTrickStrategy(new RemoveAllFilterStrategy());
            var p1 = new AIPlayer("p1", strategy)
            {
                Hand = new List<string> { "7G" }.ToCards()
            };
            var p2 = new AIPlayer("p2", new HeuristicPlayStrategy())
            {
                Hand = new List<Card>()
            };
            var p3 = new AIPlayer("p3", new HeuristicPlayStrategy())
            {
                Hand = new List<string> { "9R" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

            var action = p1.GetPlayingAction(state);

            Assert.That(action, Is.Not.Null);
            Assert.That(action.Desc, Is.EqualTo("SINGLE[7G]"));
        }

        private sealed class RemoveAllFilterStrategy : IStartingTrickFilterStrategy
        {
            public List<Trick> FilterTricks(List<Trick> tricks)
            {
                return new List<Trick>();
            }
        }

        private static int GetWeight(
            IStartingTrickWeightStrategy strategy,
            Trick trick,
            List<Trick> allSuggestedTricks,
            RoundState gameState)
        {
            return strategy.GetWeight(allSuggestedTricks, gameState)
                .Single(result => ReferenceEquals(result.Trick, trick))
                .Weight;
        }
    }
}
