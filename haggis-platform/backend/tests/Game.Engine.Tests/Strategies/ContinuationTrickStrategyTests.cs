using System.Collections.Generic;
using System.Linq;
using Haggis.AI.ContinuationTrickWeightStrategies;
using Haggis.AI.Interfaces;
using Haggis.AI.Model;
using Haggis.AI.Strategies;
using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests.Strategies
{
    [TestFixture]
    internal sealed class ContinuationTrickStrategyTests
    {
        [Test]
        public void GetPlayingAction_WhenOnlySinglesCanContinue_ShouldChooseSingle()
        {
            var strategy = new ContinuationTrickStrategy();
            var state = CreateContinuationState(
                currentPlayerHand: new List<string> { "9G", "10B" },
                openerHand: new List<string> { "8R", "2B" },
                openingTrick: new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() }));

            var action = strategy.GetPlayingAction(state);

            Assert.That(action, Is.Not.Null);
            Assert.That(action.Trick.Type, Is.EqualTo(TrickType.SINGLE));
        }

        [Test]
        public void GetPlayingAction_WhenNaturalAndWildContinuationsExist_ShouldPreferNaturalContinuation()
        {
            var strategy = new ContinuationTrickStrategy();
            var state = CreateContinuationState(
                currentPlayerHand: new List<string> { "9G", "9Y", "J" },
                openerHand: new List<string> { "8R", "8B", "2O" },
                openingTrick: new Trick(TrickType.PAIR, new List<Card> { "8R".ToCard(), "8B".ToCard() }));

            var action = strategy.GetPlayingAction(state);

            Assert.That(action.Desc, Is.EqualTo("PAIR[9G|9Y]"));
        }

        [Test]
        public void GetPlayingAction_WhenFiltersRemoveAllCandidates_ShouldReturnNull()
        {
            var strategy = new ContinuationTrickStrategy(
                continuationTrickFilterStrategies: new[] { new EmptyContinuationFilterStrategy() },
                continuationTrickWeightStrategies: new[] { new FixedContinuationWeightStrategy() });
            var state = CreateContinuationState(
                currentPlayerHand: new List<string> { "9G", "10B" },
                openerHand: new List<string> { "8R", "2B" },
                openingTrick: new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() }));

            var action = strategy.GetPlayingAction(state);

            Assert.That(action, Is.Null);
        }

        [Test]
        public void GetPlayingAction_WhenWeightsFavorSpecificCandidate_ShouldChooseHighestWeight()
        {
            var strategy = new ContinuationTrickStrategy(
                continuationTrickFilterStrategies: new[] { new PassthroughContinuationFilterStrategy() },
                continuationTrickWeightStrategies: new[] { new FixedContinuationWeightStrategy(("10B", 20), ("9G", 5)) });
            var state = CreateContinuationState(
                currentPlayerHand: new List<string> { "9G", "10B" },
                openerHand: new List<string> { "8R", "2B" },
                openingTrick: new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() }));

            var action = strategy.GetPlayingAction(state);

            Assert.That(action.Desc, Is.EqualTo("SINGLE[10B]"));
        }

        [Test]
        public void GetPlayingAction_WhenWeightsTie_ShouldPreferLowerContinuationDeterministically()
        {
            var strategy = new ContinuationTrickStrategy(
                continuationTrickFilterStrategies: new[] { new PassthroughContinuationFilterStrategy() },
                continuationTrickWeightStrategies: new[] { new FixedContinuationWeightStrategy(("10B", 10), ("9G", 10)) });
            var state = CreateContinuationState(
                currentPlayerHand: new List<string> { "9G", "10B" },
                openerHand: new List<string> { "8R", "2B" },
                openingTrick: new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() }));

            var action = strategy.GetPlayingAction(state);

            Assert.That(action.Desc, Is.EqualTo("SINGLE[9G]"));
        }

        [Test]
        public void GetPlayingAction_WhenAllContinuationCandidatesHaveNegativeWeights_ShouldChoosePass()
        {
            var strategy = new ContinuationTrickStrategy(
                continuationTrickFilterStrategies: new[] { new PassthroughContinuationFilterStrategy() },
                continuationTrickWeightStrategies: new[] { new FixedContinuationWeightStrategy(("9G", -150), ("10B", -120)) });
            var state = CreateContinuationState(
                currentPlayerHand: new List<string> { "9G", "10B" },
                openerHand: new List<string> { "8R", "2B" },
                openingTrick: new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() }));

            var action = strategy.GetPlayingAction(state);

            Assert.That(action, Is.Not.Null);
            Assert.That(action.IsPass, Is.True);
        }

        [Test]
        public void GetPlayingAction_WhenDiagnosticsEnabled_ShouldWriteContinuationBreakdown()
        {
            var diagnostics = new List<string>();
            var previousDiagnosticsSink = ContinuationTrickStrategy.DiagnosticsSink;

            try
            {
                ContinuationTrickStrategy.DiagnosticsSink = diagnostics.Add;
                var strategy = new ContinuationTrickStrategy(
                    continuationTrickFilterStrategies: new[] { new PassthroughContinuationFilterStrategy() },
                    continuationTrickWeightStrategies: new[] { new FixedContinuationWeightStrategy(("10B", 20), ("9G", 5)) });
                var state = CreateContinuationState(
                    currentPlayerHand: new List<string> { "9G", "10B" },
                    openerHand: new List<string> { "8R", "2B" },
                    openingTrick: new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() }));

                strategy.GetPlayingAction(state);

                Assert.That(diagnostics.Any(message => message.Contains("Continuation trick candidate: SINGLE[10B], weight: 20")), Is.True);
            }
            finally
            {
                ContinuationTrickStrategy.DiagnosticsSink = previousDiagnosticsSink;
            }
        }

        [Test]
        public void GetPlayingAction_WhenPassIsBestContinuation_ShouldWritePassDiagnostic()
        {
            var diagnostics = new List<string>();
            var previousDiagnosticsSink = ContinuationTrickStrategy.DiagnosticsSink;

            try
            {
                ContinuationTrickStrategy.DiagnosticsSink = diagnostics.Add;
                var strategy = new ContinuationTrickStrategy(
                    continuationTrickFilterStrategies: new[] { new PassthroughContinuationFilterStrategy() },
                    continuationTrickWeightStrategies: new[] { new FixedContinuationWeightStrategy(("9G", -150), ("10B", -120)) });
                var state = CreateContinuationState(
                    currentPlayerHand: new List<string> { "9G", "10B" },
                    openerHand: new List<string> { "8R", "2B" },
                    openingTrick: new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() }));

                strategy.GetPlayingAction(state);

                Assert.That(diagnostics.Any(message => message.Contains("Continuation trick candidate: Pass, weight: 0")), Is.True);
            }
            finally
            {
                ContinuationTrickStrategy.DiagnosticsSink = previousDiagnosticsSink;
            }
        }

        [Test]
        public void GetPlayingAction_WhenOnlyOneContinuationActionExists_ShouldStillWriteDiagnostic()
        {
            var diagnostics = new List<string>();
            var previousDiagnosticsSink = ContinuationTrickStrategy.DiagnosticsSink;

            try
            {
                ContinuationTrickStrategy.DiagnosticsSink = diagnostics.Add;
                var strategy = new ContinuationTrickStrategy();
                var state = CreateContinuationState(
                    currentPlayerHand: new List<string> { "9G" },
                    openerHand: new List<string> { "8R", "2B" },
                    openingTrick: new Trick(TrickType.SINGLE, new List<Card> { "8R".ToCard() }));

                var action = strategy.GetPlayingAction(state);

                Assert.That(action, Is.Not.Null);
                Assert.That(diagnostics.Any(message => message.Contains("Continuation trick candidate: SINGLE[9G]")), Is.True);
            }
            finally
            {
                ContinuationTrickStrategy.DiagnosticsSink = previousDiagnosticsSink;
            }
        }

        [Test]
        public void GetPlayingAction_WhenContinuationHasFollowUp_ShouldPreferConservativePairRegression()
        {
            var strategy = new ContinuationTrickStrategy();
            var state = CreateContinuationState(
                currentPlayerHand: new List<string> { "9G", "9Y", "10B" },
                openerHand: new List<string> { "8R", "8B", "2O" },
                openingTrick: new Trick(TrickType.PAIR, new List<Card> { "8R".ToCard(), "8B".ToCard() }));

            var action = strategy.GetPlayingAction(state);

            Assert.That(action.Desc, Is.EqualTo("PAIR[9G|9Y]"));
        }

        [Test]
        public void GetPlayingAction_WhenPassCompetesWithPlayableBombInEndgame_ShouldChooseBomb()
        {
            var strategy = new ContinuationTrickStrategy(
                continuationTrickFilterStrategies: new[] { new PassthroughContinuationFilterStrategy() },
                continuationTrickWeightStrategies: new IContinuationTrickWeightStrategy[]
                {
                    new PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy(1)
                });
            var state = CreateContinuationState(
                currentPlayerHand: new List<string> { "5R", "6G", "10O", "J", "Q", "K" },
                openerHand: new List<string> { "4R", "2B" },
                openingTrick: new Trick(TrickType.SINGLE, new List<Card> { "4R".ToCard() }));

            var action = strategy.GetPlayingAction(state);

            Assert.That(action, Is.Not.Null);
            Assert.That(action.IsPass, Is.False);
            Assert.That(action.Trick.Type, Is.EqualTo(TrickType.BOMB));
            Assert.That(action.Desc, Is.EqualTo("BOMB[J|Q]"));
        }

        private static RoundState CreateContinuationState(List<string> currentPlayerHand, List<string> openerHand, Trick openingTrick)
        {
            return CreateContinuationState(currentPlayerHand.ToCards(), openerHand.ToCards(), openingTrick);
        }

        private static RoundState CreateContinuationState(List<Card> currentPlayerHand, List<Card> openerHand, Trick openingTrick)
        {
            var opener = new AIPlayer("opener", new RandomPlayStrategy()) { Hand = openerHand };
            var current = new AIPlayer("current", new RandomPlayStrategy()) { Hand = currentPlayerHand };
            var third = new AIPlayer("third", new RandomPlayStrategy()) { Hand = new List<string> { "2Y", "3Y", "4Y" }.ToCards() };
            var state = new RoundState(new List<IHaggisPlayer> { opener, current, third });

            state.ApplyAction(HaggisAction.FromTrick(openingTrick, opener));
            return state;
        }

        private sealed class EmptyContinuationFilterStrategy : IContinuationTrickFilterStrategy
        {
            public List<Trick> FilterTricks(List<Trick> tricks, RoundState gameState) => new List<Trick>();
        }

        private sealed class PassthroughContinuationFilterStrategy : IContinuationTrickFilterStrategy
        {
            public List<Trick> FilterTricks(List<Trick> tricks, RoundState gameState) => tricks;
        }

        private sealed class FixedContinuationWeightStrategy : IContinuationTrickWeightStrategy
        {
            private readonly Dictionary<string, int> _weights;

            public FixedContinuationWeightStrategy(params (string TrickCard, int Weight)[] weights)
            {
                _weights = weights.ToDictionary(item => item.TrickCard, item => item.Weight);
            }

            public IReadOnlyList<(int Weight, Trick Trick)> GetWeight(List<Trick> allSuggestedTricks, RoundState gameState)
            {
                return (allSuggestedTricks ?? new List<Trick>())
                    .Select(trick =>
                    {
                        var key = trick.Cards.First().ToString();
                        return (_weights.TryGetValue(key, out var weight) ? weight : 0, trick);
                    })
                    .ToList();
            }
        }
    }
}
