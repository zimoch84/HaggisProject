using System.Collections.Generic;
using System.Linq;
using Haggis.AI.ContinuationTrickWeightStrategies;
using Haggis.AI.Model;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
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
        public void PenalizeWildCardsInContinuation_ShouldCapPenaltyAtMinusFifty()
        {
            var strategy = new PenalizeWildCardsInContinuationWeightStrategy(7);
            var wildSequence = new Trick(TrickType.SEQ3, new List<Card>
            {
                "J".ToCard().WildAs("9R".ToCard()),
                "Q".ToCard().WildAs("10R".ToCard()),
                "K".ToCard().WildAs("JR".ToCard())
            });
            var p1 = new AIPlayer("p1")
            {
                Hand = new List<string>
                {
                    "2R", "2B", "3R", "3B", "4R", "4B", "5R", "5B", "6R",
                    "6B", "7R", "7B", "8R", "8B", "J", "Q", "K"
                }.ToCards()
            };
            var p2 = new AIPlayer("p2") { Hand = new List<string> { "8G", "8Y", "8O" }.ToCards() };
            var p3 = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });

            var weights = strategy.GetWeight(new List<Trick> { wildSequence }, state);

            Assert.That(weights.Single().Weight, Is.EqualTo(-50));
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

        [Test]
        public void PreferLowerValueContinuation_WhenLowerAlternativesExist_ShouldReturnPositiveWeightForLowerContinuation()
        {
            var strategy = new PreferLowerValueContinuationWeightStrategy(2);
            var pair9 = new Trick(TrickType.PAIR, new List<Card> { "9R".ToCard(), "9B".ToCard() });
            var pair10 = new Trick(TrickType.PAIR, new List<Card> { "10R".ToCard(), "10B".ToCard() });
            var lastPair8 = new Trick(TrickType.PAIR, new List<Card> { "8R".ToCard(), "8B".ToCard() });
            var p1 = new AIPlayer("p1") { Hand = new List<string> { "9R", "9B", "10R", "10B" }.ToCards() };
            var p2 = new AIPlayer("p2") { Hand = new List<string> { "8R", "8B" }.ToCards() };
            var p3 = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(lastPair8, p2));

            var weights = strategy.GetWeight(new List<Trick> { pair9, pair10 }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pair9)).Weight, Is.GreaterThan(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pair9)).Weight, Is.GreaterThan(weights.Single(result => ReferenceEquals(result.Trick, pair10)).Weight));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, pair10)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferLowerValueContinuation_WhenBombIsAvailable_ShouldNotProduceNegativeWeights()
        {
            var strategy = new PreferLowerValueContinuationWeightStrategy(2);
            var single9 = new Trick(TrickType.SINGLE, new List<Card> { "9R".ToCard() });
            var bomb = new Trick(TrickType.BOMB, new List<Card> { "J".ToCard(), "Q".ToCard() });
            var lastSingle8 = new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() });
            var p1 = new AIPlayer("p1") { Hand = new List<string> { "9R" }.ToCards() };
            var p2 = new AIPlayer("p2") { Hand = new List<string> { "8R" }.ToCards() };
            var p3 = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(lastSingle8, p2));

            var weights = strategy.GetWeight(new List<Trick> { single9, bomb }, state);

            Assert.That(weights.All(result => result.Weight >= 0), Is.True);
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, single9)).Weight, Is.EqualTo(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, bomb)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferLowerValueContinuation_ShouldCapMaximumWeightAtFifty()
        {
            var strategy = new PreferLowerValueContinuationWeightStrategy(2);
            var single9 = new Trick(TrickType.SINGLE, new List<Card> { "9R".ToCard() });
            var single10 = new Trick(TrickType.SINGLE, new List<Card> { "10R".ToCard() });
            var singleJ = new Trick(TrickType.SINGLE, new List<Card> { "J".ToCard() });
            var lastSingle8 = new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() });
            var p1 = new AIPlayer("p1") { Hand = new List<string> { "9R", "10R", "J" }.ToCards() };
            var p2 = new AIPlayer("p2") { Hand = new List<string> { "8R" }.ToCards() };
            var p3 = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(lastSingle8, p2));

            var weights = strategy.GetWeight(new List<Trick> { single9, single10, singleJ }, state);

            Assert.That(weights.All(result => result.Weight <= 50), Is.True);
        }

        [Test]
        public void PreferUsingWildAsHigherCardInContinuation_WhenWildCanBeMovedHigher_ShouldPenalizeLowerWildPlacement()
        {
            var strategy = new PreferUsingWildAsHigherCardInContinuationWeightStrategy(4);
            var lowerSequence = new Trick(TrickType.SEQ3, new List<Card> { "7G".ToCard(), "8R".ToCard(), "9B".ToCard() });
            var higherSequenceWithWild = new Trick(TrickType.SEQ3, new List<Card>
            {
                "8R".ToCard(),
                "9B".ToCard(),
                "J".ToCard().WildAs("10G".ToCard())
            });
            var lastSequence = new Trick(TrickType.SEQ3, new List<Card> { "6G".ToCard(), "7R".ToCard(), "8B".ToCard() });
            var p1 = new AIPlayer("p1") { Hand = new List<string> { "7G", "8R", "9B", "J" }.ToCards() };
            var p2 = new AIPlayer("p2") { Hand = new List<string> { "6G", "7R", "8B" }.ToCards() };
            var p3 = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(lastSequence, p2));

            var weights = strategy.GetWeight(new List<Trick> { lowerSequence, higherSequenceWithWild }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, lowerSequence)).Weight, Is.EqualTo(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, higherSequenceWithWild)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferUsingWildAsHigherCardInContinuation_WithSimplifiedHand_ShouldPenalizeUsingWildAsLowerSequenceCard()
        {
            var strategy = new PreferUsingWildAsHigherCardInContinuationWeightStrategy(4);
            var lowerSequence = new Trick(TrickType.SEQ3, new List<Card>
            {
                "J".ToCard().WildAs("7R".ToCard()),
                "8B".ToCard(),
                "9B".ToCard()
            });
            var higherSequence = new Trick(TrickType.SEQ3, new List<Card>
            {
                "8B".ToCard(),
                "9B".ToCard(),
                "J".ToCard().WildAs("10B".ToCard())
            });
            var alternativeSuitSequence = new Trick(TrickType.SEQ3, new List<Card>
            {
                "J".ToCard().WildAs("7G".ToCard()),
                "8G".ToCard(),
                "9G".ToCard()
            });
            var lastSequence = new Trick(TrickType.SEQ3, new List<Card> { "6O".ToCard(), "7O".ToCard(), "8O".ToCard() });
            var p1 = new AIPlayer("p1") { Hand = new List<string> { "7R", "8B", "9B", "8G", "9G", "J", "3G", "5Y" }.ToCards() };
            var p2 = new AIPlayer("p2") { Hand = new List<string> { "6O", "7O", "8O" }.ToCards() };
            var p3 = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(lastSequence, p2));

            var weights = strategy.GetWeight(new List<Trick> { lowerSequence, higherSequence, alternativeSuitSequence }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, lowerSequence)).Weight, Is.LessThan(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, higherSequence)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void PreferUsingWildAsHigherCardInContinuation_WithSimplifiedHand_ShouldNotPenalizeHigherSequenceThatKeepsLowerNaturalCard()
        {
            var strategy = new PreferUsingWildAsHigherCardInContinuationWeightStrategy(4);
            var lowerSequence = new Trick(TrickType.SEQ3, new List<Card>
            {
                "J".ToCard().WildAs("7R".ToCard()),
                "8B".ToCard(),
                "9B".ToCard()
            });
            var higherSequence = new Trick(TrickType.SEQ3, new List<Card>
            {
                "8B".ToCard(),
                "9B".ToCard(),
                "J".ToCard().WildAs("10B".ToCard())
            });
            var lastSequence = new Trick(TrickType.SEQ3, new List<Card> { "6O".ToCard(), "7O".ToCard(), "8O".ToCard() });
            var p1 = new AIPlayer("p1") { Hand = new List<string> { "7R", "8B", "9B", "J" }.ToCards() };
            var p2 = new AIPlayer("p2") { Hand = new List<string> { "6O", "7O", "8O" }.ToCards() };
            var p3 = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { p1, p2, p3 });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(lastSequence, p2));

            var weights = strategy.GetWeight(new List<Trick> { lowerSequence, higherSequence }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, higherSequence)).Weight, Is.EqualTo(0));
        }

        [Test]
        public void GeneratedContinuations_WithReproducedHand_ShouldContainBothLowerAndHigherSequenceAlternatives()
        {
            var (state, generatedTricks) = CreateReproducedAi2MoveThreeState();

            Assert.That(state.CurrentTrickPlay.LastNotPassTrick.ToString(), Is.EqualTo("SEQ3[6O|7O|8O]"));
            Assert.That(generatedTricks.Select(trick => trick.ToString()), Does.Contain("SEQ3[J[7B]|8B|9B]"));
            Assert.That(generatedTricks.Select(trick => trick.ToString()), Does.Contain("SEQ3[8B|9B|J[10B]]"));
        }

        [Test]
        public void PreferUsingWildAsHigherCardInContinuation_WithGeneratedContinuationsFromReproducedHand_ShouldOnlyPenalizeLowerSequence()
        {
            var strategy = new PreferUsingWildAsHigherCardInContinuationWeightStrategy(10);
            var (state, generatedTricks) = CreateReproducedAi2MoveThreeState();
            var weights = strategy.GetWeight(generatedTricks, state);
            var lowerSequenceWeight = weights.Single(result => result.Trick.ToString() == "SEQ3[J[7B]|8B|9B]").Weight;
            var higherSequenceWeight = weights.Single(result => result.Trick.ToString() == "SEQ3[8B|9B|J[10B]]").Weight;

            TestContext.WriteLine($"lowerSequenceWeight={lowerSequenceWeight}");
            TestContext.WriteLine($"higherSequenceWeight={higherSequenceWeight}");

            Assert.That(lowerSequenceWeight, Is.LessThan(0));
            Assert.That(higherSequenceWeight, Is.EqualTo(0));
        }

        [Test]
        public void PreferUsingWildAsHigherCardInContinuation_WithGeneratedContinuationsFromReproducedHand_ShouldRankHigherWildPlacementAboveLowerOne()
        {
            var wildPlacementStrategy = new PreferUsingWildAsHigherCardInContinuationWeightStrategy(10);
            var wildPenaltyStrategy = new PenalizeWildCardsInContinuationWeightStrategy(7);
            var lowerValueStrategy = new PreferLowerValueContinuationWeightStrategy(2);
            var followUpStrategy = new PreferContinuationsWithFollowUpWeightStrategy(3);
            var (state, generatedTricks) = CreateReproducedAi2MoveThreeState();

            var lowerSequence = generatedTricks.Single(trick => trick.ToString() == "SEQ3[J[7B]|8B|9B]");
            var higherSequence = generatedTricks.Single(trick => trick.ToString() == "SEQ3[8B|9B|J[10B]]");

            var totalWeights = new[]
            {
                wildPenaltyStrategy.GetWeight(generatedTricks, state),
                wildPlacementStrategy.GetWeight(generatedTricks, state),
                lowerValueStrategy.GetWeight(generatedTricks, state),
                followUpStrategy.GetWeight(generatedTricks, state)
            }
            .SelectMany(result => result)
            .GroupBy(result => result.Trick)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Weight));

            Assert.That(totalWeights[higherSequence], Is.GreaterThan(totalWeights[lowerSequence]));
        }

        [Test]
        public void PreferUsingWildAsHigherCardInContinuation_WhenWildRanksAreIdentical_ShouldReturnZeroForBoth()
        {
            var strategy = new PreferUsingWildAsHigherCardInContinuationWeightStrategy(4);
            var first = new Trick(TrickType.PAIR, new List<Card>
            {
                "9B".ToCard(),
                "J".ToCard().WildAs("9G".ToCard())
            });
            var second = new Trick(TrickType.PAIR, new List<Card>
            {
                "9R".ToCard(),
                "Q".ToCard().WildAs("9Y".ToCard())
            });
            var state = new RoundState(new List<IHaggisPlayer>
            {
                new AIPlayer("p1") { Hand = new List<string> { "9B", "J", "9R", "Q" }.ToCards() },
                new AIPlayer("p2") { Hand = new List<string> { "8B", "8R" }.ToCards() },
                new AIPlayer("p3") { Hand = new List<Card>() }
            });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(new Trick(TrickType.PAIR, new List<Card> { "8B".ToCard(), "8R".ToCard() }), state.Players[1]));

            var weights = strategy.GetWeight(new List<Trick> { first, second }, state);

            Assert.That(weights.All(result => result.Weight == 0), Is.True);
        }

        [Test]
        public void PreferUsingWildAsHigherCardInContinuation_ShouldWorkForSameKindContinuation()
        {
            var strategy = new PreferUsingWildAsHigherCardInContinuationWeightStrategy(4);
            var lowerPair = new Trick(TrickType.PAIR, new List<Card>
            {
                "J".ToCard().WildAs("9G".ToCard()),
                "10B".ToCard()
            });
            var higherPair = new Trick(TrickType.PAIR, new List<Card>
            {
                "10B".ToCard(),
                "J".ToCard().WildAs("10G".ToCard())
            });
            var state = new RoundState(new List<IHaggisPlayer>
            {
                new AIPlayer("p1") { Hand = new List<string> { "10B", "J" }.ToCards() },
                new AIPlayer("p2") { Hand = new List<string> { "9B", "9R" }.ToCards() },
                new AIPlayer("p3") { Hand = new List<Card>() }
            });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(new Trick(TrickType.PAIR, new List<Card> { "9B".ToCard(), "9R".ToCard() }), state.Players[1]));

            var weights = strategy.GetWeight(new List<Trick> { lowerPair, higherPair }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, lowerPair)).Weight, Is.LessThan(0));
            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, higherPair)).Weight, Is.EqualTo(0));
        }

        private static (RoundState State, List<Trick> GeneratedTricks) CreateReproducedAi2MoveThreeState()
        {
            var moveGenerationService = new MoveGenerationService();
            var aiPlayer = new AIPlayer("AI-2")
            {
                Hand = new List<string>
                {
                    "2O", "3G", "4R", "5Y", "6B", "6Y", "7R", "8B", "8G", "8Y",
                    "9B", "9G", "10R", "10Y", "J", "Q", "K"
                }.ToCards()
            };
            var opponent = new AIPlayer("AI-1")
            {
                Hand = new List<string> { "6O", "7O", "8O" }.ToCards()
            };
            var spectator = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { aiPlayer, opponent, spectator });
            var lastTrick = new Trick(TrickType.SEQ3, new List<Card> { "6O".ToCard(), "7O".ToCard(), "8O".ToCard() });
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(lastTrick, opponent));

            var generatedTricks = moveGenerationService.GetPossibleContinuationTricks(aiPlayer, state.CurrentTrickPlay.LastNotPassTrick);
            return (state, generatedTricks);
        }
    }
}
