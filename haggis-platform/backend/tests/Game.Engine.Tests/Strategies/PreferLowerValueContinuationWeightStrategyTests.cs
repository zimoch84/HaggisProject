using System.Collections.Generic;
using System.Linq;
using Haggis.AI.ContinuationTrickWeightStrategies;
using Haggis.AI.Model;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests.Strategies
{
    [TestFixture]
    internal sealed class PreferLowerValueContinuationWeightStrategyTests
    {
        [Test]
        public void PreferLowerValueContinuation_WhenLowerAlternativesExist_ShouldReturnPositiveWeightForLowerContinuation()
        {
            var strategy = new PreferLowerValueContinuationWeightStrategy(1);
            var pair9 = "9RB_PAIR".ToTrick();
            var pair10 = "10RB_PAIR".ToTrick();
            var state = CreateState(
                currentPlayerHand: new List<string> { "9R", "9B", "10R", "10B" },
                openerHand: new List<string> { "8R", "8B" },
                openingTrick: "8RB_PAIR".ToTrick());

            var weights = strategy.GetWeight(new List<Trick> { pair9, pair10 }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pair9)).Weight, Is.GreaterThan(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pair9)).Weight, Is.GreaterThan(weights.Single(result => ReferenceEquals(result.Trick, pair10)).Weight));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pair10)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferLowerValueContinuation_WhenBombIsAvailable_ShouldNotProduceNegativeWeights()
        {
            var strategy = new PreferLowerValueContinuationWeightStrategy(1);
            var single9 = "9R_SINGLE".ToTrick();
            var bomb = new Trick(TrickType.BOMB, new List<Card> { "J".ToCard(), "Q".ToCard() });
            var state = CreateState(
                currentPlayerHand: new List<string> { "9R" },
                openerHand: new List<string> { "8R" },
                openingTrick: "8R_SINGLE".ToTrick());

            var weights = strategy.GetWeight(new List<Trick> { single9, bomb }, state);

            Assert.That(weights.All(result => result.Weight >= 0), Is.True);
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, single9)).Weight, Is.EqualTo(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, bomb)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferLowerValueContinuation_ShouldScaleWeightToComparableOpportunity()
        {
            var strategy = new PreferLowerValueContinuationWeightStrategy(1);
            var single9 = "9R_SINGLE".ToTrick();
            var single10 = "10R_SINGLE".ToTrick();
            var singleJ = "J_SINGLE".ToTrick();
            var state = CreateState(
                currentPlayerHand: new List<string> { "9R", "10R", "J" },
                openerHand: new List<string> { "8R" },
                openingTrick: "8R_SINGLE".ToTrick());

            var weights = strategy.GetWeight(new List<Trick> { single9, single10, singleJ }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, single9)).Weight, Is.EqualTo(20));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, single10)).Weight, Is.EqualTo(10));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, singleJ)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferLowerValueContinuation_ShouldUseBuiltInBaseScale()
        {
            var strategy = new PreferLowerValueContinuationWeightStrategy(1);
            var single9 = "9R_SINGLE".ToTrick();
            var single10 = "10R_SINGLE".ToTrick();
            var singleJ = "J_SINGLE".ToTrick();
            var state = CreateState(
                currentPlayerHand: new List<string> { "9R", "10R", "J" },
                openerHand: new List<string> { "8R" },
                openingTrick: "8R_SINGLE".ToTrick());

            var weights = strategy.GetWeight(new List<Trick> { single9, single10, singleJ }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, single9)).Weight, Is.EqualTo(20));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, single10)).Weight, Is.EqualTo(10));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, singleJ)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferLowerValueContinuation_WithHand10G9G8G_ShouldReturnZeroWhenOnlyOneComparableContinuationExists()
        {
            var strategy = new PreferLowerValueContinuationWeightStrategy(1);
            var sequence = "8G_SEQ3".ToTrick();
            var state = CreateState(
                currentPlayerHand: new List<string> { "10G", "9G", "8G" },
                openerHand: new List<string> { "7R", "8R", "9R" },
                openingTrick: "7R_SEQ3".ToTrick());

            var weights = strategy.GetWeight(new List<Trick> { sequence }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, sequence)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferLowerValueContinuation_WithReproducedAi1Hand_ShouldReturnZeroForAllSameRankTenPairs()
        {
            var strategy = new PreferLowerValueContinuationWeightStrategy(1);
            var pairTenBg = "10BG_PAIR".ToTrick();
            var pairTenBo = "10BO_PAIR".ToTrick();
            var pairTenGo = "10GO_PAIR".ToTrick();
            var state = CreateState(
                currentPlayerHand: new List<string> { "10G", "5R", "10B", "10O", "3Y", "J", "Q", "K" },
                openerHand: new List<string> { "9R", "9Y" },
                openingTrick: "9RY_PAIR".ToTrick());

            var weights = strategy.GetWeight(new List<Trick> { pairTenBg, pairTenBo, pairTenGo }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pairTenBg)).Weight, Is.EqualTo(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pairTenBo)).Weight, Is.EqualTo(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pairTenGo)).Weight, Is.EqualTo(0));
        }

        private static RoundState CreateState(List<string> currentPlayerHand, List<string> openerHand, Trick openingTrick)
        {
            var opener = new AIPlayer("opener") { Hand = openerHand.ToCards() };
            var current = new AIPlayer("current") { Hand = currentPlayerHand.ToCards() };
            var third = new AIPlayer("third") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { opener, current, third });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(openingTrick, opener));
            return state;
        }
    }
}
