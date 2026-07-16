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
    internal sealed class PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategyTests
    {
        [Test]
        public void GetWeight_WhenBombIsPlayableInEndgameAndPassIsLegal_ShouldBonusBomb()
        {
            var strategy = new PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy(1);
            var bomb = new Trick(TrickType.BOMB, new List<Card> { "J".ToCard(), "Q".ToCard() });
            var state = CreateState(
                currentPlayerHand: new List<string> { "5R", "6G", "10O", "J", "Q", "K" },
                openerHand: new List<string> { "4R", "2B" },
                openingTrick: "4R_SINGLE".ToTrick());

            var weights = strategy.GetWeight(new List<Trick> { bomb }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, bomb)).Weight, Is.EqualTo(100));
        }

        [Test]
        public void GetWeight_WhenHandIsNotInEndgame_ShouldNotBonusBomb()
        {
            var strategy = new PreferNotPassingWhenHoldingPlayableBombInEndgameWeightStrategy(1);
            var bomb = new Trick(TrickType.BOMB, new List<Card> { "J".ToCard(), "Q".ToCard() });
            var state = CreateState(
                currentPlayerHand: new List<string> { "2R", "3R", "4R", "5R", "6R", "10O", "J", "Q", "K" },
                openerHand: new List<string> { "4B", "2G" },
                openingTrick: "4B_SINGLE".ToTrick());

            var weights = strategy.GetWeight(new List<Trick> { bomb }, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, bomb)).Weight, Is.EqualTo(0));
        }

        private static RoundState CreateState(List<string> currentPlayerHand, List<string> openerHand, Trick openingTrick)
        {
            var opener = new AIPlayer("opener") { Hand = openerHand.ToCards() };
            var current = new AIPlayer("current") { Hand = currentPlayerHand.ToCards() };
            var third = new AIPlayer("third") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { opener, current, third });
            state.ApplyAction(HaggisAction.FromTrick(openingTrick, opener));
            return state;
        }
    }
}
