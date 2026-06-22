using System.Collections.Generic;
using System.Linq;
using Haggis.AI.ContinuationTrickWeightStrategies;
using Haggis.AI.Model;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
using NUnit.Framework;

namespace HaggisTests.Strategies
{
    [TestFixture]
    internal sealed class PreferContinuationsWithFollowUpWeightStrategyTests
    {
        [Test]
        public void PreferContinuationsWithFollowUp_WithReproducedAi1MoveFiveHand_ShouldReturnBoundedWeightForSeq3_6O7O8O()
        {
            var strategy = new PreferContinuationsWithFollowUpWeightStrategy(3);
            var (state, generatedTricks) = CreateReproducedAi1MoveFiveState();
            var targetTrick = generatedTricks.Single(trick => trick.ToString() == "SEQ3[6O|7O|8O]");

            var weights = strategy.GetWeight(generatedTricks, state);

            Assert.That(weights.Single(result => ReferenceEquals(result.Trick, targetTrick)).Weight, Is.EqualTo(0));
        }

        private static (RoundState State, List<Trick> GeneratedTricks) CreateReproducedAi1MoveFiveState()
        {
            var moveGenerationService = new MoveGenerationService();
            var aiPlayer = new AIPlayer("AI-1")
            {
                Hand = new List<string>
                {
                    "4G", "10G", "7O", "8O", "5R", "9R", "4Y", "6O", "10B",
                    "6Y", "9Y", "10O", "6G", "3Y", "J", "Q", "K"
                }.ToCards()
            };
            var opponent = new AIPlayer("opener")
            {
                Hand = new List<string> { "5B", "6B", "7B" }.ToCards()
            };
            var spectator = new AIPlayer("p3") { Hand = new List<Card>() };
            var state = new RoundState(new List<IHaggisPlayer> { aiPlayer, opponent, spectator });
            var lastTrick = "5B_SEQ3".ToTrick();
            state.CurrentTrickPlay.Actions.Add(HaggisAction.FromTrick(lastTrick, opponent));

            var generatedTricks = moveGenerationService.GetPossibleContinuationTricks(
                aiPlayer,
                state.CurrentTrickPlay.LastNotPassTrick);
            return (state, generatedTricks);
        }
    }
}
