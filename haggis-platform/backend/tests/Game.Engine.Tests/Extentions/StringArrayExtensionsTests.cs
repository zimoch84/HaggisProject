using Haggis.Extentions;
using Haggis.Enums;
using Haggis.Model;
using NUnit.Framework;
using System;

namespace HaggisTests

{

    [TestFixture]
    public class StringArrayExtensionsTests
    {
        [Test]
        public void ToTricks_ValidInputs_ReturnsTrickArray()
        {
            // Arrange
            string[] trickStrings = { "2Y_SEQ3", "4RY_PAIR", "3B_SEQ5", "4B_SEQ5" };

            // Act
            Trick[] tricks = trickStrings.ToTricks();

            // Assert
            Assert.That(tricks.Length, Is.EqualTo(4));
            Assert.That(tricks[0].Type, Is.EqualTo(TrickType.SEQ3));
            Assert.That(tricks[1].Type, Is.EqualTo(TrickType.PAIR));
            Assert.That(tricks[2].Type, Is.EqualTo(TrickType.SEQ5));
            Assert.That(tricks[3].Type, Is.EqualTo(TrickType.SEQ5));
        }

        [Test]
        public void ToTricks_InvalidInput_ThrowsException()
        {
            // Arrange
            string[] trickStrings = { "InvalidInput" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => trickStrings.ToTricks());
        }
    }
}
