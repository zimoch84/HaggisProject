using static Haggis.Domain.Enums.Suit;
using static Haggis.Domain.Enums.Rank;
using Haggis.Domain.Extentions;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests
{
    [TestFixture()]
    public class CardTests
    {
        [Test()]
        public void CompareToTest()
        {
            Card CardTwoBlack = new Card(TWO, BLACK);
            Card CardThreeBlack = new Card(THREE,   BLACK);
            Card CardEightBlack = new Card(EIGHT, BLACK);
            Card CardTenBlack = new Card(TEN, BLACK);

            Card CardTwoRed = new Card(TWO, RED);
            Card CardThreeRed = new Card(THREE, RED);
            Card CardEightRed = new Card(EIGHT, RED);
            Card CardTenRed = new Card(TEN, RED);
                
            Card CardJack = new Card(JACK);
            Card CardQueen = new Card(QUEEN);
            Card CardKing = new Card(QUEEN);


            Assert.That(CardTwoBlack.CompareTo(CardKing) , Is.LessThan(0));
            Assert.That(CardTwoBlack.CompareTo(CardQueen), Is.LessThan(0));
            Assert.That(CardTwoBlack.CompareTo(CardJack), Is.LessThan(0));
            Assert.That(CardTwoBlack.CompareTo(CardTenBlack), Is.LessThan(0));
            Assert.That(CardTwoBlack.CompareTo(CardEightBlack), Is.LessThan(0));
            Assert.That(CardTwoBlack.CompareTo(CardThreeBlack), Is.LessThan(0));


            Assert.That(CardKing.CompareTo(CardKing), Is.EqualTo(0));
            Assert.That(CardTwoRed.CompareTo(CardTwoBlack), Is.EqualTo(0));
            Assert.That(CardThreeRed.CompareTo(CardThreeBlack), Is.EqualTo(0));
            Assert.That(CardEightRed.CompareTo(CardEightBlack), Is.EqualTo(0));
            Assert.That(CardTenBlack.CompareTo(CardTenRed), Is.EqualTo(0));


            Assert.That(CardTenRed.CompareTo(CardQueen), Is.LessThan(0));
            Assert.That(CardTenRed.CompareTo(CardJack), Is.LessThan(0));
            Assert.That(CardTenRed.CompareTo(CardTenBlack), Is.EqualTo(0));
            Assert.That(CardTenRed.CompareTo(CardEightBlack) , Is.GreaterThan(0));
            Assert.That(CardTenRed.CompareTo(CardThreeBlack), Is.GreaterThan(0));


        }
        [Test()]
        public void CompareToWhenWildCardReplaceAnother()
        {
            Card CardJack = new Card(JACK);
            Card CardQueen = new Card(QUEEN);
            Card CardKing = Rank.KING.ToCard().WildAs("2B".ToCard());

            Card CardThreeBlack = new Card(THREE, BLACK);
            Card CardEightBlack = new Card(EIGHT, BLACK);
            Card CardTenBlack = new Card(TEN, BLACK);


            Assert.That(CardKing.CompareTo(CardQueen), Is.LessThan(0));
            Assert.That(CardKing.CompareTo(CardJack), Is.LessThan(0));
            Assert.That(CardKing.CompareTo(CardTenBlack), Is.LessThan(0));
            Assert.That(CardKing.CompareTo(CardEightBlack), Is.LessThan(0));
            Assert.That(CardKing.CompareTo(CardThreeBlack), Is.LessThan(0));

        }


        [Test()]
        public void ShouldCreateCard()
        {

            Card CardTwoBlack = new Card(TWO, BLACK);
            Card CardThreeBlack = new Card(THREE, BLACK);
            Card CardEightBlack = new Card(EIGHT, BLACK);
            Card CardKing = new Card(KING);
            Card CardJack = new Card(JACK, YELLOW);
            Card CardQueen = new Card(QUEEN);

            Assert.That(CardTwoBlack, Is.Not.Null );
            Assert.That(CardThreeBlack, Is.Not.Null);
            Assert.That(CardEightBlack, Is.Not.Null);
            Assert.That(CardKing, Is.Not.Null);
            Assert.That(CardJack, Is.Not.Null);
            Assert.That(CardQueen, Is.Not.Null);
        
        }
        [TestCase("2O", "2O")]
        public void ShouldEquals(string card1, string card2) {
  
            Assert.That(card1.ToCard(), Is.EqualTo(card2.ToCard()));
        } 
        
        [TestCase("2O", "3O")]
        [TestCase("2O", "2Y")]
        [TestCase("4G", "J")]
        [TestCase("K", "J")]
        public void ShouldNotEquals(string card1, string card2) {

            Assert.That(card1.ToCard(), Is.Not.EqualTo(card2.ToCard()));
        } 
        
        [Test]
        public void ShouldEqualsWheUsedAsWildTests() {

            var card1 = new Card(JACK);
            var card2 = new Card(JACK).WildAs("2O".ToCard());
       
            Assert.That(card1, Is.EqualTo(card2));
        }
    }
}