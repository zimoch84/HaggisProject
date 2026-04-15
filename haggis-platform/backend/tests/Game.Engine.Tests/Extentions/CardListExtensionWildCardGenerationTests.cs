using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using NUnit.Framework;
using System.Linq;

namespace HaggisTests
{
    [TestFixture]
    public class CardListExtensionWildCardGenerationTests
    {
        [Test]
        public void FindTheSameCardsWithWildCards_ShouldFind_Triple_WithOneCardAndTwoWilds()
        {
            var cards = new[] { "10Y", "J", "Q" };

            var combinations = cards.ToCards().FindTheSameCardsWithWildCards(TrickType.TRIPLE);

            Assert.That(combinations.Count, Is.EqualTo(1));
            Assert.That(combinations[0].Type, Is.EqualTo(TrickType.TRIPLE));
            Assert.That(
                combinations[0].Cards.Select(card => card.ToString()),
                Is.EqualTo(new[] { "10Y", "J[10]", "Q[10]" }));
        }

        [Test]
        public void FindTheSameCardsWithWildCards_ShouldFind_Quad_WithOneCardAndThreeWilds()
        {
            var cards = new[] { "10Y", "J", "Q", "K" };

            var combinations = cards.ToCards().FindTheSameCardsWithWildCards(TrickType.QUAD);

            Assert.That(combinations.Count, Is.EqualTo(1));
            Assert.That(combinations[0].Type, Is.EqualTo(TrickType.QUAD));
            Assert.That(
                combinations[0].Cards.Select(card => card.ToString()),
                Is.EqualTo(new[] { "10Y", "J[10]", "Q[10]", "K[10]" }));
        }
    }
}
