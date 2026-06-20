using System.Collections.Generic;
using System.Collections;
using System.Linq;
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
        private static IEnumerable HeuristicOpeningHandsWithAllWeightStrategies()
        {
            yield return new TestCaseData(
                new List<string> { "2R", "2B", "2G", "3R", "3B", "3G", "4R", "4B", "4G", "5R", "6G", "7R", "8R", "9R", "J", "Q", "K" },
                null,
                "TRIPLE",
                new[] { "2R", "2B", "2G" })
                .SetName("GetPlayingAction_WhenHandHas14Cards_ShouldUseDefaultWeightsAndPreferTriple2");

            yield return new TestCaseData(
                new List<string> { "2R", "2B", "2G", "5G", "6R", "6O", "7G" },
                null,
                "SINGLE",
                new[] { "7G" })
                .SetName("GetPlayingAction_WhenHandHas7Cards_ShouldPreferHigherSingleAfterCutoff");

            yield return new TestCaseData(
                new List<string> { "2R", "2B", "10R" },
                null,
                "SINGLE",
                new[] { "10R" })
                .SetName("GetPlayingAction_WhenHandHas3Cards_ShouldPreferSafeSingleOverPair");

            yield return new TestCaseData(
                new List<string> { "2R", "2B", "5G", "6R", "6O" },
                null,
                "PAIR",
                new[] { "2R", "2B" })
                .SetName("GetPlayingAction_WhenHandHas5Cards_ShouldPreferPairOverSafeSingle");

            yield return new TestCaseData(
                new List<string> { "2R", "2B", "8G", "10R" },
                null,
                "SINGLE",
                new[] { "10R" })
                .SetName("GetPlayingAction_WhenHandHas4CardsAndPair_ShouldPreferHigherSingleLate");

            yield return new TestCaseData(
                new List<string> { "7G" },
                null,
                "SINGLE",
                new[] { "7G" })
                .SetName("GetPlayingAction_WhenHandHas1Card_ShouldPlayOnlyCard");
        }

        [Test]
        public void GetPlayingAction_WhenSafeSingleExists_ShouldPreferSingleThatDoesNotBreakCombinations()
        {
            var p1 = new AIPlayer("p1", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "2R", "2B", "2G", "5G", "6R", "6O", "7G" }.ToCards()
            };
            var p2 = new AIPlayer("p2", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "3R" }.ToCards()
            };
            var p3 = new AIPlayer("p3", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "4R" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Desc, Is.EqualTo("SINGLE[7G]"));
        }

        [Test]
        public void GetPlayingAction_WhenUsingBenchmarkOpeningHand_ShouldPreferTripleOverPair()
        {
            var p1 = new AIPlayer("p1", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string>
                {
                    "2R", "2B", "2G", "3B", "4B", "5G", "6R", "6B", "6G", "6O", "7G", "9G", "9Y", "10B", "J", "Q", "K"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "2Y", "2O", "3G", "3O", "4R", "4G", "5O", "6Y", "7R", "7B", "8B", "9O", "10G", "10O", "J", "Q", "K" }.ToCards()
            };
            var p3 = new AIPlayer("p3", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "3R", "3Y", "4Y", "4O", "5R", "5B", "7O", "8R", "8Y", "8O", "9R", "9B", "10R", "10Y", "J", "Q", "K" }.ToCards()
            };
            var state = new RoundState(
                new List<IHaggisPlayer> { p1, p2, p3 },
                haggisCards: new List<string> { "5Y", "7Y", "8G" }.ToCards());

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Desc, Is.EqualTo("TRIPLE[2R|2B|2G]"));
        }

        [Test]
        public void GetPlayingAction_WhenHandContainsOpeningBomb_ShouldAvoidBombAsOpening()
        {
            var p1 = new AIPlayer("p1", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string>
                {
                    "2R", "2B", "2G", "3G", "3O", "4B", "4Y", "4O", "5G", "5Y", "7G", "8G", "9R", "9Y", "J", "Q", "K"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "2Y", "3R", "5B", "6R", "6Y", "7R", "8R", "8B", "8Y", "9B", "9G", "9O", "10Y", "10O", "J", "Q", "K" }.ToCards()
            };
            var p3 = new AIPlayer("p3", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "2O", "3Y", "4R", "4G", "5R", "5O", "6B", "7B", "7Y", "7O", "8O", "10R", "10B", "10G", "J", "Q", "K" }.ToCards()
            };
            var state = new RoundState(
                new List<IHaggisPlayer> { p1, p2, p3 },
                haggisCards: new List<string> { "3B", "6G", "6O" }.ToCards());

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Trick.Type, Is.Not.EqualTo(Haggis.Domain.Enums.TrickType.BOMB));
        }

        [Test]
        public void GetPlayingAction_WhenHandContainsNaturalPairs_ShouldPreferPairOpening()
        {
            var p1 = new AIPlayer("p1", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string>
                {
                    "2B", "2G", "3B", "4B", "6B", "6O", "7G", "10B"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "2Y", "2O", "4R", "5O", "6Y", "7R", "7B", "8B", "9O", "10G" }.ToCards()
            };
            var p3 = new AIPlayer("p3", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "3R", "4Y", "4O", "5B", "8Y", "9R" }.ToCards()
            };
            var state = new RoundState(
                new List<IHaggisPlayer> { p1, p2, p3 },
                haggisCards: new List<string> { "5Y", "7Y", "8G" }.ToCards());

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Desc, Is.Not.EqualTo("SINGLE[2B]"));
            Assert.That(action.Desc, Is.Not.EqualTo("SINGLE[2G]"));
            Assert.That(action.Desc, Is.Not.EqualTo("SINGLE[6B]"));
            Assert.That(action.Desc, Is.Not.EqualTo("SINGLE[6O]"));
            Assert.That(action.Desc, Is.EqualTo("PAIR[2B|2G]"));
        }

        [TestCaseSource(nameof(HeuristicOpeningHandsWithAllWeightStrategies))]
        public void GetPlayingAction_WithDefaultWeights_ShouldChooseExpectedOpeningForDifferentHandSizes(
            List<string> hand,
            HeuristicOptions heuristicOptions,
            string expectedTrickType,
            string[] expectedCards)
        {
            var p1 = new AIPlayer("p1", HeuristicPlayStrategy.Create(heuristicOptions: heuristicOptions))
            {
                Hand = hand.ToCards()
            };
            var p2 = new AIPlayer("p2", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "10R" }.ToCards()
            };
            var p3 = new AIPlayer("p3", HeuristicPlayStrategy.Create())
            {
                Hand = new List<string> { "9B" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

            var action = p1.GetPlayingAction(state);

            Assert.That(action.Trick.Type.ToString(), Is.EqualTo(expectedTrickType));
            if (expectedCards != null)
            {
                Assert.That(action.Trick.Cards.Select(card => card.ToString()), Is.EqualTo(expectedCards));
            }
        }
    }
}
