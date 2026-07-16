using NUnit.Framework;
using Haggis.Domain.Extentions;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaggisTests
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [TestCase("2Y", Rank.TWO, Suit.YELLOW)]
        [TestCase("3O", Rank.THREE, Suit.ORANGE)]
        [TestCase("10G", Rank.TEN, Suit.GREEN)]
        public void ToCardTest(string card, Rank expectedRank, Suit expectedSuit)
        {
            Assert.That(card.ToCard().Rank.Equals(expectedRank), Is.True);
            Assert.That(card.ToCard().Suit.Equals(expectedSuit), Is.True);
        }


        [TestCase("J", Rank.JACK)]
        [TestCase("Q", Rank.QUEEN)]
        [TestCase("K", Rank.KING)]

        public void ToCardTest(string card, Rank expectedRank)
        {
            Assert.That(card.ToCard().Rank.Equals(expectedRank), Is.True);
        }

        [Test]
        public void ToTrick_ValidInput_ReturnsTrick()
        {
            // Arrange
            string trickString = "2Y_SEQ3";

            // Act
            Trick trick = trickString.ToTrick();

            // Assert
            Assert.That(trick.Type, Is.EqualTo(TrickType.SEQ3));
            Assert.That(trick.Cards[0].Rank, Is.EqualTo(Rank.TWO));
            Assert.That(trick.Cards[0].Suit, Is.EqualTo(Suit.YELLOW));
        }

        [Test]
        public void ToTrick_WhenPairStartsWithTen_ShouldParseRankAndSuitsCorrectly()
        {
            var trick = "10RB_PAIR".ToTrick();

            Assert.That(trick.Type, Is.EqualTo(TrickType.PAIR));
            Assert.That(trick.Cards.Select(card => card.ToString()), Is.EqualTo(new[] { "10R", "10B" }));
        }

        [Test]
        public void ToTrick_WhenSingleIsWildWithoutSuit_ShouldParseSingleWildCard()
        {
            var trick = "J_SINGLE".ToTrick();

            Assert.That(trick.Type, Is.EqualTo(TrickType.SINGLE));
            Assert.That(trick.Cards.Select(card => card.ToString()), Is.EqualTo(new[] { "J" }));
        }

        [Test]
        public void ToTrick_InvalidInput_ThrowsException()
        {
            // Arrange
            string trickString = "InvalidInput";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => trickString.ToTrick());
        }

        [Test]
        public void ToCards() {

            var cards = "[2O|3O|4O|6B|6G|7Y|7G|7O|8G|9B|9R|10O|10Y|10B|J|Q|K]".ToCards();

            Assert.That(cards, Is.InstanceOf<List<Card>>()); // Verify result type
            Assert.That(cards.Count, Is.GreaterThan(0)); // Verify list is not empty

            // Verify a few representative values
            Assert.That(cards[0], Is.EqualTo(new Card(Rank.TWO, Suit.ORANGE))); // First card
            Assert.That(cards[1], Is.EqualTo(new Card(Rank.THREE, Suit.ORANGE))); // Second card
            Assert.That(cards[2], Is.EqualTo(new Card(Rank.FOUR, Suit.ORANGE))); // Third card
        }

    }
}
