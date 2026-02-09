using NUnit.Framework;
using Haggis.Extentions;
using Haggis.Enums;
using Haggis.Model;
using System;
using System.Collections.Generic;

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

            Assert.That(cards, Is.InstanceOf<List<Card>>()); // Sprawdź, czy jest listą Card
            Assert.That(cards.Count, Is.GreaterThan(0)); // Sprawdź, czy lista nie jest pusta

            // Sprawdzenie zawartości listy, możesz dodać więcej asercji w zależności od oczekiwań
            Assert.That(cards[0], Is.EqualTo(new Card(Rank.TWO, Suit.ORANGE))); // Przykład - sprawdź pierwszą kartę
            Assert.That(cards[1], Is.EqualTo(new Card(Rank.THREE, Suit.ORANGE))); // Sprawdź drugą kartę
            Assert.That(cards[2], Is.EqualTo(new Card(Rank.FOUR, Suit.ORANGE))); // Sprawdź trzecią kartę
        }

    }
}