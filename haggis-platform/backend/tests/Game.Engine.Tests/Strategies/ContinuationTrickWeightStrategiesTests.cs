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
    internal sealed class ContinuationTrickWeightStrategiesTests
    {
        [Test]
        public void PenalizeWildCardsInContinuation_WhenNaturalAndWildAlternativesExist_ShouldPreferNatural()
        {
            var strategy = new PenalizeWildCardsInContinuationWeightStrategy(7);
            var naturalPair = new Trick(TrickType.PAIR, new List<Card> { "9G".ToCard(), "9Y".ToCard() });
            var wildPair = new Trick(TrickType.PAIR, new List<Card>
            {
                "9G".ToCard(),
                "J".ToCard().WildAs("9Y".ToCard())
            });
            var p1 = new AIPlayer("p1") { Hand = new List<string> { "9G", "9Y", "J" }.ToCards() };
            var p2 = new AIPlayer("p2") { Hand = new List<string> { "8R", "8B" }.ToCards() };
            var p3 = new AIPlayer("p3") { Hand = new List<string> { "2O" }.ToCards() };
            var state = new RoundState(new List<IHaggisPlayer> { p2, p1, p3 });

            var weights = strategy.GetWeight(new List<Trick> { naturalPair, wildPair }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, naturalPair)).Weight, Is.EqualTo(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, wildPair)).Weight, Is.LessThan(0));
        }

        [Test]
        public void PenalizeContinuationWhenHigherRelatedCombinationExists_WhenTrickBreaksLargerSet_ShouldPenalizeSmallerTrick()
        {
            var strategy = new PenalizeContinuationWhenHigherRelatedCombinationExistsWeightStrategy(4);
            var pair = new Trick(TrickType.PAIR, new List<Card> { "6R".ToCard(), "6B".ToCard() });
            var triple = new Trick(TrickType.TRIPLE, new List<Card> { "6R".ToCard(), "6B".ToCard(), "6G".ToCard() });
            var p1 = new AIPlayer("p1") { Hand = new List<string> { "6R", "6B", "6G", "9Y" }.ToCards() };
            var p2 = new AIPlayer("p2") { Hand = new List<string> { "5R", "5B" }.ToCards() };
            var p3 = new AIPlayer("p3") { Hand = new List<string> { "2O" }.ToCards() };
            var state = new RoundState(new List<IHaggisPlayer> { p2, p1, p3 });

            var weights = strategy.GetWeight(new List<Trick> { pair, triple }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pair)).Weight, Is.LessThan(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, triple)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferContinuationsWithFollowUp_WhenTrickHasMoreFollowUps_ShouldReturnHigherWeight()
        {
            var strategy = new PreferContinuationsWithFollowUpWeightStrategy(3);
            var pair2 = new Trick(TrickType.PAIR, new List<Card> { "2R".ToCard(), "2B".ToCard() });
            var pair3 = new Trick(TrickType.PAIR, new List<Card> { "3R".ToCard(), "3B".ToCard() });
            var pair4 = new Trick(TrickType.PAIR, new List<Card> { "4R".ToCard(), "4B".ToCard() });
            var p1 = new AIPlayer("p1") { Hand = new List<string> { "2R", "2B", "3R", "3B", "4R", "4B" }.ToCards() };
            var p2 = new AIPlayer("p2") { Hand = new List<Card>() };
            var p3 = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

            var weights = strategy.GetWeight(new List<Trick> { pair2, pair3, pair4 }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pair2)).Weight, Is.GreaterThan(weights.Single(result => ReferenceEquals(result.Trick, pair3)).Weight));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pair3)).Weight, Is.GreaterThan(weights.Single(result => ReferenceEquals(result.Trick, pair4)).Weight));
        }
    }
}
