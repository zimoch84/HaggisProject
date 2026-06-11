using System.Collections.Generic;
using System.Linq;
using Haggis.AI.Model;
using Haggis.AI.StartingTrickWeightStrategies;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests.Strategies
{
    [TestFixture]
    internal sealed class PreferTricksThatAreMostLikelyNonBreakableWeightStrategyTests
    {
        private static IEnumerable<TestCaseData> FullyBlockedNinesCases()
        {
            yield return new TestCaseData(
                TrickType.PAIR,
                new[] { "9G", "9Y" })
                .SetName("GetWeight_WhenAllTensAreInDiscardAndHandHasPairOfNines_ShouldReturnFifty");

            yield return new TestCaseData(
                TrickType.TRIPLE,
                new[] { "9R", "9G", "9Y" })
                .SetName("GetWeight_WhenAllTensAreInDiscardAndHandHasTripleOfNines_ShouldReturnFifty");

            yield return new TestCaseData(
                TrickType.QUAD,
                new[] { "9R", "9B", "9G", "9Y" })
                .SetName("GetWeight_WhenAllTensAreInDiscardAndHandHasQuadOfNines_ShouldReturnFifty");
        }

        private static IEnumerable<TestCaseData> NinesBlockedByTensButWildsRemainCases()
        {
            yield return new TestCaseData(
                TrickType.PAIR,
                new[] { "9G", "9Y" })
                .SetName("GetWeight_WhenAllTensAreInDiscardButWildsRemainAndHandHasPairOfNines_ShouldReturnZero");

            yield return new TestCaseData(
                TrickType.TRIPLE,
                new[] { "9R", "9G", "9Y" })
                .SetName("GetWeight_WhenAllTensAreInDiscardButWildsRemainAndHandHasTripleOfNines_ShouldReturnZero");

            yield return new TestCaseData(
                TrickType.QUAD,
                new[] { "9R", "9B", "9G", "9Y" })
                .SetName("GetWeight_WhenAllTensAreInDiscardButWildsRemainAndHandHasQuadOfNines_ShouldReturnZero");
        }

        private static IEnumerable<TestCaseData> WildSubstitutedNinesCases()
        {
            yield return new TestCaseData(
                TrickType.PAIR,
                new[] { "9G" },
                "9Y",
                0)
                .SetName("GetWeight_WhenAllTensAreInDiscardAndPairOfNinesUsesWild_ShouldReturnZero");

            yield return new TestCaseData(
                TrickType.TRIPLE,
                new[] { "9R", "9G" },
                "9Y",
                50)
                .SetName("GetWeight_WhenAllTensAreInDiscardAndTripleOfNinesUsesWild_ShouldReturnFifty");

            yield return new TestCaseData(
                TrickType.QUAD,
                new[] { "9R", "9B", "9G" },
                "9Y",
                50)
                .SetName("GetWeight_WhenAllTensAreInDiscardAndQuadOfNinesUsesWild_ShouldReturnFifty");
        }

        [Test]
        public void GetWeight_WhenDiscardPileAndCurrentTrickPlayAreEmpty_ShouldReturnZeroForEveryTrick()
        {
            var strategy = new PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(50);
            var p1 = new AIPlayer("p1")
            {
                Hand = new List<string>
                {
                    "2R", "2B", "2G", "3B", "4B", "5G", "6R", "6B", "6G", "6O", "7G", "9G", "9Y", "10B", "J", "Q", "K"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2") { Discard = new List<Card>() };
            var p3 = new AIPlayer("p3") { Discard = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            var tricks = new List<Trick>
            {
                new Trick(TrickType.SINGLE, new List<Card> { "10B".ToCard() }),
                new Trick(TrickType.PAIR, new List<Card> { "9G".ToCard(), "9Y".ToCard() }),
                new Trick(TrickType.TRIPLE, new List<Card> { "2R".ToCard(), "2B".ToCard(), "2G".ToCard() }),
                new Trick(TrickType.QUAD, new List<Card> { "6R".ToCard(), "6B".ToCard(), "6G".ToCard(), "6O".ToCard() })
            };

            var weights = strategy.GetWeight(tricks, state);

            Assert.That(weights.Select(result => result.Weight), Is.All.Zero);
        }

        [Test]
        public void GetWeight_WhenTrickIsSingle_ShouldReturnZero()
        {
            var strategy = new PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(50);
            var p1 = new AIPlayer("p1")
            {
                Hand = new List<string> { "3B", "4B", "5G", "6G", "6O", "7G" }.ToCards()
            };
            var p2 = new AIPlayer("p2")
            {
                Discard = new List<string> { "8B", "10G", "10O" }.ToCards()
            };
            var p3 = new AIPlayer("p3")
            {
                Discard = new List<string> { "9R", "9B", "8R", "8O", "8Y" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            var trick = new Trick(TrickType.SINGLE, new List<Card> { "3B".ToCard() });

            var weight = strategy.GetWeight(new List<Trick> { trick }, state)
                .Single(result => ReferenceEquals(result.Trick, trick))
                .Weight;

            Assert.That(weight, Is.Zero);
        }

        [Test]
        public void GetWeight_WhenTrickIsPair_ShouldReturnZero()
        {
            var strategy = new PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(50);
            var p1 = new AIPlayer("p1")
            {
                Hand = new List<string> { "3B", "4B", "5G", "6G", "6O", "7G" }.ToCards()
            };
            var p2 = new AIPlayer("p2")
            {
                Discard = new List<string> { "8B", "10G", "10O", "7R", "Q" }.ToCards()
            };
            var p3 = new AIPlayer("p3")
            {
                Discard = new List<string> { "9R", "9B", "8R", "8O", "8Y", "J", "K" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            var trick = new Trick(TrickType.PAIR, new List<Card> { "6G".ToCard(), "6O".ToCard() });

            var weight = strategy.GetWeight(new List<Trick> { trick }, state)
                .Single(result => ReferenceEquals(result.Trick, trick))
                .Weight;

            Assert.That(weight, Is.Zero);
        }

        [Test]
        public void GetWeight_WhenAllHigherQuadsAreUnavailable_ShouldReturnFifty()
        {
            var strategy = new PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(50);
            var trick = new Trick(TrickType.QUAD, new List<Card>
            {
                "6R".ToCard(),
                "6B".ToCard(),
                "6G".ToCard(),
                "6O".ToCard()
            });
            var p1 = new AIPlayer("p1")
            {
                Hand = new List<string> { "4B", "6R", "6B", "6G", "6O", "7G", "9G", "9Y" }.ToCards()
            };
            var p2 = new AIPlayer("p2")
            {
                Discard = new List<string>
                {
                    "7R", "7B", "7Y", "7O",
                    "8R", "8B", "8Y", "8O", "8G",
                    "9R", "9B", "9O",
                    "10R", "10B", "10G", "10O",
                    "J", "Q", "K"
                }.ToCards()
            };
            var p3 = new AIPlayer("p3")
            {
                Discard = new List<Card>()
            };
            var state = new RoundState(
                new List<IHaggisPlayer> { p1, p2, p3 },
                haggisCards: new List<string> { "5Y", "7Y", "8G" }.ToCards());

            var weights = strategy.GetWeight(new List<Trick> { trick }, state);
            var weight = weights.Single(result => ReferenceEquals(result.Trick, trick)).Weight;

            Assert.That(weight, Is.EqualTo(50));
        }

        [TestCaseSource(nameof(FullyBlockedNinesCases))]
        public void GetWeight_WhenAllTensAreInDiscardAndHandHasNinesCombination_ShouldReturnFifty(
            TrickType trickType,
            string[] trickCards)
        {
            var strategy = new PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(50);
            var trick = new Trick(trickType, trickCards.ToCards());
            var p1 = new AIPlayer("p1")
            {
                Hand = new List<string>
                {
                    "4B", "7G", "9R", "9B", "9G", "9Y"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2")
            {
                Discard = new List<string>
                {
                    "10R", "10B", "10G", "10O",
                    "J", "Q", "K"
                }.ToCards()
            };
            var p3 = new AIPlayer("p3")
            {
                Discard = new List<Card>()
            };
            var state = new RoundState(
                new List<IHaggisPlayer> { p1, p2, p3 },
                haggisCards: new List<string> { "5Y", "7Y", "8G" }.ToCards());

            var weights = strategy.GetWeight(new List<Trick> { trick }, state);
            var weight = weights.Single(result => ReferenceEquals(result.Trick, trick)).Weight;

            Assert.That(weight, Is.EqualTo(50));
        }

        [TestCaseSource(nameof(NinesBlockedByTensButWildsRemainCases))]
        public void GetWeight_WhenAllTensAreInDiscardButWildsRemainAndHandHasNinesCombination_ShouldReturnZero(
            TrickType trickType,
            string[] trickCards)
        {
            var strategy = new PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(50);
            var trick = new Trick(trickType, trickCards.ToCards());
            var p1 = new AIPlayer("p1")
            {
                Hand = new List<string>
                {
                    "4B", "7G", "9R", "9B", "9G", "9Y"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2")
            {
                Discard = new List<string>
                {
                    "10R", "10B", "10G", "10O"
                }.ToCards()
            };
            var p3 = new AIPlayer("p3")
            {
                Discard = new List<Card>()
            };
            var state = new RoundState(
                new List<IHaggisPlayer> { p1, p2, p3 },
                haggisCards: new List<string> { "5Y", "7Y", "8G" }.ToCards());

            var weights = strategy.GetWeight(new List<Trick> { trick }, state);
            var weight = weights.Single(result => ReferenceEquals(result.Trick, trick)).Weight;

            Assert.That(weight, Is.Zero);
        }

        [TestCaseSource(nameof(WildSubstitutedNinesCases))]
        public void GetWeight_WhenAllTensAreInDiscardAndNinesCombinationUsesWild_ShouldMatchExpectedWeight(
            TrickType trickType,
            string[] naturalCards,
            string replacedCard,
            int expectedWeight)
        {
            var strategy = new PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(50);
            var trickCards = naturalCards.ToCards();
            trickCards.Add("J".ToCard().WildAs(replacedCard.ToCard()));
            var trick = new Trick(trickType, trickCards);
            var p1 = new AIPlayer("p1")
            {
                Hand = new List<string>
                {
                    "4B", "7G", "9R", "9B", "9G", "9Y", "J"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2")
            {
                Discard = new List<string>
                {
                    "10R", "10B", "10G", "10O",
                    "Q", "K"
                }.ToCards()
            };
            var p3 = new AIPlayer("p3")
            {
                Discard = new List<Card>()
            };
            var state = new RoundState(
                new List<IHaggisPlayer> { p1, p2, p3 },
                haggisCards: new List<string> { "5Y", "7Y", "8G" }.ToCards());

            var weights = strategy.GetWeight(new List<Trick> { trick }, state);
            var weight = weights.Single(result => ReferenceEquals(result.Trick, trick)).Weight;

            Assert.That(weight, Is.EqualTo(expectedWeight));
        }

        [Test]
        public void GetWeight_WhenUsingVisibleCardsFromLoggedTrickFiveState_ShouldMatchExpectedWeights()
        {
            var strategy = new PreferTricksThatAreMostLikelyNonBreakableWeightStrategy(50);
            var p1 = new AIPlayer("p1")
            {
                Hand = new List<string> { "3B", "4B", "5G", "6G", "6O", "7G" }.ToCards(),
                Discard = new List<string>
                {
                    "2R", "2B", "2G",
                    "10B", "J", "Q",
                    "9G", "9Y",
                    "6R", "6B",
                    "K"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2")
            {
                Hand = new List<string> { "2Y", "2O", "3G", "3O", "4R", "4G", "5O", "6Y", "7B", "9O" }.ToCards(),
                Discard = new List<string>
                {
                    "8B", "J", "K",
                    "10G", "10O",
                    "7R", "Q"
                }.ToCards()
            };
            var p3 = new AIPlayer("p3")
            {
                Hand = new List<string> { "3R", "4Y", "4O", "10R", "10Y" }.ToCards(),
                Discard = new List<string>
                {
                    "9R", "J", "Q",
                    "3Y", "5R", "7O", "9B",
                    "5B", "K",
                    "8R", "8O",
                    "8Y"
                }.ToCards()
            };
            var state = new RoundState(
                new List<IHaggisPlayer> { p1, p2, p3 },
                haggisCards: new List<string> { "5Y", "7Y", "8G" }.ToCards());

            var single3B = new Trick(TrickType.SINGLE, new List<Card> { "3B".ToCard() });
            var pair6 = new Trick(TrickType.PAIR, new List<Card> { "6G".ToCard(), "6O".ToCard() });
            var seq3 = new Trick(TrickType.SEQ3, new List<Card> { "5G".ToCard(), "6G".ToCard(), "7G".ToCard() });

            var weights = strategy.GetWeight(new List<Trick> { single3B, pair6, seq3 }, state);

            Assert.That(
                weights.Single(result => ReferenceEquals(result.Trick, single3B)).Weight,
                Is.EqualTo(0));
            Assert.That(
                weights.Single(result => ReferenceEquals(result.Trick, pair6)).Weight,
                Is.EqualTo(0));
            Assert.That(
                weights.Single(result => ReferenceEquals(result.Trick, seq3)).Weight,
                Is.Zero);
        }
    }
}
