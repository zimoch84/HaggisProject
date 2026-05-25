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

        [Test]
        public void FindPairedSequences_ShouldUseWildCardsToCompleteConsecutivePairs()
        {
            var cards = new[] { "3G", "4B", "J", "Q" };

            var combinations = cards.ToCards().FindPairedSequences(TrickType.PAIRSEQ2);

            Assert.That(combinations.Count, Is.EqualTo(1));
            Assert.That(combinations[0].Type, Is.EqualTo(TrickType.PAIRSEQ2));
            Assert.That(
                combinations[0].Cards.Select(card => card.ToString()),
                Is.EqualTo(new[] { "J[3]", "3G", "4B", "Q[4]" }));
        }

        [Test]
        public void FindCardSequences_ShouldAllowSequenceStartingAtTenWithJackAndQueen()
        {
            var cards = new[] { "10R", "J", "Q" };

            var combinations = cards.ToCards().FindCardSequences(TrickType.SEQ3);

            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("SEQ3[10R|J[11]|Q[12]]"));
        }

        [Test]
        public void FindCardSequences_ShouldAllowAnyWildCardsForJackAndQueenSlots()
        {
            var cards = new[] { "10R", "Q", "K" };

            var combinations = cards.ToCards().FindCardSequences(TrickType.SEQ3);

            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("SEQ3[10R|Q[11]|K[12]]"));
        }

        [Test]
        public void FindPairedSequences_ShouldFindThreePairSequence()
        {
            var cards = new[] { "6O", "7B", "8B", "6B", "7O", "8O" };

            var combinations = cards.ToCards().FindPairedSequences(TrickType.PAIRSEQ3);

            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("PAIRSEQ3[6B|6O|7B|7O|8B|8O]"));
        }

        [Test]
        public void FindPairedSequences_ShouldAllowTenAsPairSequenceStartWithWildCards()
        {
            var cards = new[] { "10G", "10B", "J", "Q" };

            var combinations = cards.ToCards().FindPairedSequences(TrickType.PAIRSEQ2);

            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("PAIRSEQ2[10B|10G|J[11]|Q[11]]"));
        }
 
    }
}
