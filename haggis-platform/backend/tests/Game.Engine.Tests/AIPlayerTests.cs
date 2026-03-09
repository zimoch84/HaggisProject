using Haggis.AI.Model;
using Haggis.Domain.Extentions;
using NUnit.Framework;
using System.Collections.Generic;

namespace HaggisTests
{
    [TestFixture]
    internal class AIPlayerTests
    {
        [Test]
        public void SuggestedTricks_ShouldReturnOnlyComparableTricks_ForAIPlayer()
        {
            var aiPlayer = new AIPlayer("AI");
            aiPlayer.Hand = new List<string> { "2Y", "4Y", "9G" }.ToCards();
            var lastTrick = "3O_SINGLE".ToTrick();

            var suggested = aiPlayer.SuggestedTricks(lastTrick);

            Assert.That(suggested, Is.Not.Empty);
            Assert.That(suggested.TrueForAll(trick => trick.CompareTo(lastTrick) > 0), Is.True);
        }
    }
}
