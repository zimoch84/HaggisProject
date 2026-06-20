using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;
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
                Is.EqualTo(new[] { "10Y", "J[10Y]", "Q[10Y]" }));
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
                Is.EqualTo(new[] { "10Y", "J[10Y]", "Q[10Y]", "K[10Y]" }));
        }

        [Test]
        public void FindPairedSequences_ShouldUseWildCardsToCompleteConsecutivePairs()
        {
            var cards = new[] { "3G", "4B", "J", "Q" };

            var combinations = cards.ToCards().FindPairedSequences(TrickType.PAIRSEQ2);

            Assert.That(combinations.Count, Is.EqualTo(2));
            Assert.That(combinations.All(trick => trick.Type == TrickType.PAIRSEQ2), Is.True);
            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("PAIRSEQ2[J[3B]|3G|4B|Q[4G]]"));
            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("PAIRSEQ2[Q[3B]|3G|4B|J[4G]]"));
        }

        [Test]
        public void FindStairs_ShouldFind_TripleStair()
        {
            var cards = new[] { "3R", "3B", "3G", "4R", "4B", "4G", "5R", "5B", "5G" };

            var combinations = cards.ToCards().FindStairs(TrickType.TRIPLESTAIR3);

            var expected = new Trick(TrickType.TRIPLESTAIR3, cards.ToCards());

            Assert.That(combinations.Count, Is.EqualTo(1));
            Assert.That(combinations[0], Is.EqualTo(expected));
        }

        [Test]
        public void FindStairs_ShouldFind_QuadStair()
        {
            var cards = new[] { "3R", "3B", "3G", "3Y", "4R", "4B", "4G", "4Y" };

            var combinations = cards.ToCards().FindStairs(TrickType.QUADSTAIR2);

            var expected = new Trick(TrickType.QUADSTAIR2, cards.ToCards());

            Assert.That(combinations.Count, Is.EqualTo(1));
            Assert.That(combinations[0], Is.EqualTo(expected));
        }

        [Test]
        public void FindCardSequences_ShouldAllowSequenceStartingAtTenWithJackAndQueen()
        {
            var cards = new[] { "10R", "J", "Q" };

            var combinations = cards.ToCards().FindCardSequences(TrickType.SEQ3);

            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("SEQ3[10R|J[J]|Q[Q]]"));
        }

        [Test]
        public void FindCardSequences_ShouldAllowAnyWildCardsForJackAndQueenSlots()
        {
            var cards = new[] { "10R", "Q", "K" };

            var combinations = cards.ToCards().FindCardSequences(TrickType.SEQ3);

            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("SEQ3[10R|Q[J]|K[Q]]"));
        }

        [Test]
        public void FindCardSequences_ShouldGenerateSequenceForQueen_WhenJackIsAlsoAvailable()
        {
            var cards = new[] { "7R", "9R", "10R", "J", "Q" };

            var combinations = cards.ToCards().FindCardSequences(TrickType.SEQ4);

            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("SEQ4[7R|Q[8R]|9R|10R]"));
            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("SEQ4[7R|J[8R]|9R|10R]"));
        }

        [Test]
        public void FindCardSequences_ShouldReturnOnlyRequestedSequenceType()
        {
            var cards = new[] { "3R", "4R", "5R", "6R", "7R" };

            var combinations = cards.ToCards().FindCardSequences(TrickType.SEQ4);

            Assert.That(combinations, Is.Not.Empty);
            Assert.That(combinations.All(trick => trick.Type == TrickType.SEQ4), Is.True);
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
        public void FindPairedSequences_ShouldGenerateThreePairSequenceForTwoTwosTwoFoursAndTwoWilds()
        {
            var cards = new[] { "2G", "2R", "4G", "4R", "J", "Q" };

            var combinations = cards.ToCards().FindPairedSequences(TrickType.PAIRSEQ3);
            var matchingCombination = combinations.SingleOrDefault(trick =>
                trick.Cards.Count == 6 &&
                trick.Cards[0].ToString() == "2R" &&
                trick.Cards[1].ToString() == "2G" &&
                trick.Cards[4].ToString() == "4R" &&
                trick.Cards[5].ToString() == "4G");

            Assert.That(matchingCombination, Is.Not.Null);
            Assert.That(matchingCombination!.Type, Is.EqualTo(TrickType.PAIRSEQ3));
            Assert.That(
                matchingCombination.Cards.Select(card => card.ToString()),
                Is.EqualTo(new[] { "2R", "2G", "J[3R]", "Q[3G]", "4R", "4G" }));
            Assert.That(matchingCombination.Cards[2].Replaces?.Suit, Is.EqualTo(Suit.RED));
            Assert.That(matchingCombination.Cards[3].Replaces?.Suit, Is.EqualTo(Suit.GREEN));
        }

        [Test]
        public void FindStairs_ShouldReturnOnlyRequestedStairType()
        {
            var cards = new[] { "3R", "3B", "3G", "4R", "4B", "4G", "5R", "5B", "5G" };

            var combinations = cards.ToCards().FindStairs(TrickType.TRIPLESTAIR3);

            Assert.That(combinations, Is.Not.Empty);
            Assert.That(combinations.All(trick => trick.Type == TrickType.TRIPLESTAIR3), Is.True);
        }

        [Test]
        public void FindPairedSequences_ShouldAllowTenAsPairSequenceStartWithWildCards()
        {
            var cards = new[] { "10G", "10B", "J", "Q" };

            var combinations = cards.ToCards().FindPairedSequences(TrickType.PAIRSEQ2);

            Assert.That(
                combinations.Select(trick => trick.ToString()),
                Does.Contain("PAIRSEQ2[10B|10G|J[J]|Q[J]]"));
        }

        [Test]
        public void FindPairedSequences_ShouldGenerateDifferentWildAssignments_ForEquivalentGapFill()
        {
            var cards = new[] { "2R", "2G", "3R", "3G", "5R", "5G", "Q", "K" };

            var combinations = cards.ToCards().FindPairedSequences(TrickType.PAIRSEQ4);
            var combinationStrings = combinations
                .Select(trick => trick.ToString())
                .ToList();

            Assert.That(
                combinationStrings,
                Does.Contain("PAIRSEQ4[2R|2G|3R|3G|Q[4R]|K[4G]|5R|5G]"));
        }
 
    }
}
