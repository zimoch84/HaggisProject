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
    internal sealed class PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategyTests
    {
        [Test]
        public void PenalizeOpeningWhenHigherRelatedCombinationExists_WhenSingleCanGrowIntoPair_ShouldSubtractPenaltyBasedOnHandSize()
        {
            var strategy = new PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy(20);
            var trick = new Trick(TrickType.SINGLE, new List<Card> { "2B".ToCard() });
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2B", "2G", "3B", "4B", "6B", "6O", "7G", "10B" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });
            var tricks = new List<Trick>
            {
                trick,
                new Trick(TrickType.PAIR, new List<Card> { "2B".ToCard(), "2G".ToCard() }),
                new Trick(TrickType.PAIR, new List<Card> { "6B".ToCard(), "6O".ToCard() })
            };

            var weight = GetWeight(strategy, trick, tricks, state);

            Assert.That(weight, Is.EqualTo(-(8 * 20)));
        }

        [Test]
        public void PenalizeOpeningWhenHigherRelatedCombinationExists_WhenSeq3CanGrowIntoSeq4_ShouldSubtractPenaltyBasedOnHandSize()
        {
            var strategy = new PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy(20);
            var trick = "2R_SEQ3".ToTrick();
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2R", "3R", "4R", "5R", "9B" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });
            var tricks = new List<Trick>
            {
                trick,
                "2R_SEQ4".ToTrick()
            };

            var weight = GetWeight(strategy, trick, tricks, state);

            Assert.That(weight, Is.EqualTo(-(5 * 20)));
        }

        [Test]
        public void PenalizeOpeningWhenHigherRelatedCombinationExists_WhenPairCanGrowIntoTriple_ShouldSubtractPenaltyBasedOnHandSize()
        {
            var strategy = new PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy(20);
            var trick = new Trick(TrickType.PAIR, new List<Card> { "2R".ToCard(), "2B".ToCard() });
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2R", "2B", "2G", "7G" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });
            var tricks = new List<Trick>
            {
                trick,
                new Trick(TrickType.TRIPLE, new List<Card> { "2R".ToCard(), "2B".ToCard(), "2G".ToCard() })
            };

            var weight = GetWeight(strategy, trick, tricks, state);

            Assert.That(weight, Is.EqualTo(-(4 * 20)));
        }

        [Test]
        public void PenalizeOpeningWhenHigherRelatedCombinationExists_WhenPairStairs2CanGrowIntoPairStairs3_ShouldSubtractPenaltyBasedOnHandSize()
        {
            var strategy = new PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy(20);
            var trick = new Trick(
                TrickType.PAIRSEQ2,
                new List<Card> { "2R".ToCard(), "2B".ToCard(), "3R".ToCard(), "3B".ToCard() });
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2R", "2B", "3R", "3B", "4R", "4B", "9G" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });
            var tricks = new List<Trick>
            {
                trick,
                new Trick(
                    TrickType.PAIRSEQ3,
                    new List<Card> { "2R".ToCard(), "2B".ToCard(), "3R".ToCard(), "3B".ToCard(), "4R".ToCard(), "4B".ToCard() })
            };

            var weight = GetWeight(strategy, trick, tricks, state);

            Assert.That(weight, Is.EqualTo(-(7 * 20)));
        }

        [Test]
        public void PenalizeOpeningWhenHigherRelatedCombinationExists_WhenOnlyHigherCombinationUsesWilds_ShouldReturnZero()
        {
            var strategy = new PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy(20);
            var trick = new Trick(TrickType.TRIPLE, new List<Card> { "2R".ToCard(), "2B".ToCard(), "2G".ToCard() });
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2R", "2B", "2G", "J" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });
            var tricks = new List<Trick>
            {
                trick,
                new Trick(TrickType.QUAD, new List<Card>
                {
                    "2R".ToCard(),
                    "2B".ToCard(),
                    "2G".ToCard(),
                    "J".ToCard().WildAs("2R".ToCard())
                })
            };

            var weight = GetWeight(strategy, trick, tricks, state);

            Assert.That(weight, Is.Zero);
        }

        [Test]
        public void PenalizeOpeningWhenHigherRelatedCombinationExists_WhenSingleIsNotPartOfHigherRelatedCombination_ShouldReturnZero()
        {
            var strategy = new PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy(20);
            var trick = new Trick(TrickType.SINGLE, new List<Card> { "2G".ToCard() });
            var player = new AIPlayer("p1")
            {
                Hand = new List<string> { "2G", "3B", "4B", "5G", "6R", "6B", "6G", "6O", "7G", "10B" }.ToCards()
            };
            var state = new RoundState(new List<IHaggisPlayer> { player, new AIPlayer("p2"), new AIPlayer("p3") });
            var tricks = new List<Trick>
            {
                trick,
                new Trick(TrickType.SINGLE, new List<Card> { "3B".ToCard() }),
                new Trick(TrickType.PAIR, new List<Card> { "6R".ToCard(), "6B".ToCard() }),
                new Trick(TrickType.TRIPLE, new List<Card> { "6R".ToCard(), "6B".ToCard(), "6G".ToCard() }),
                new Trick(TrickType.QUAD, new List<Card> { "6R".ToCard(), "6B".ToCard(), "6G".ToCard(), "6O".ToCard() }),
                "3B_SEQ3".ToTrick(),
                "5G_SEQ3".ToTrick()
            };

            var weight = GetWeight(strategy, trick, tricks, state);

            Assert.That(weight, Is.Zero);
        }

        private static int GetWeight(
            PenalizeOpeningWhenHigherRelatedCombinationExistsWeightStrategy strategy,
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
