using Haggis.Enums;
using Haggis.Extentions;
using Haggis.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace HaggisTests
{
    [TestFixture]
    public class CardListExctentionTests
    {
        [Test]
        public void IsBomb_IsBomb_Should_Return_True_For_Bomb_Combination()
        {
            // Arrange
            var hand = new List<Card>
        {
            new Card(Rank.THREE, Suit.YELLOW),
            new Card(Rank.FIVE, Suit.RED),
            new Card(Rank.SEVEN, Suit.ORANGE),
            new Card(Rank.NINE, Suit.GREEN)
        };

            // Act
            var result = hand.IsBomb();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsBomb_IsBomb_Should_Return_False_For_Non_Bomb_Combination()
        {
            var hand = new List<Card>
        {
            new Card(Rank.THREE, Suit.BLACK),
            new Card(Rank.FIVE, Suit.YELLOW),
            new Card(Rank.SEVEN, Suit.ORANGE),
            new Card(Rank.NINE, Suit.ORANGE) 
        };

            // Act
            var result = hand.IsBomb();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsBomb_IsBomb_Should_Return_False_For_Too_Few_Cards()
        {
            // Arrange
            var hand = new List<Card>
        {
            new Card(Rank.THREE, Suit.YELLOW),
            new Card(Rank.FIVE, Suit.RED),
            new Card(Rank.SEVEN, Suit.ORANGE)
        };

            // Act
            var result = hand.IsBomb();

            // Assert
            Assert.That(result, Is.False);
        }

        [TestCase(new string[] { "3Y", "5O", "7R", "9B" }, 1)]
        [TestCase(new string[] { "3Y", "3O", "7Y", "9B" }, 1)]
        [TestCase(new string[] { "3Y", "3O", "3B", "9B" }, 1)]
        [TestCase(new string[] { "3Y", "3R", "5O", "7B", "9G" }, 5)]
        [TestCase(new string[] { "3Y", "3G", "5O", "5Y", "7Y", "9B" }, 15)]
        [TestCase(new string[] { "3Y", "3G", "5Y", "5G", "7Y", "7G", "9Y", "9G" }, 70)]
        public void GetKCombinationsByRank_ShouldReturn_XKCombination_of_4_cards_By_Rank(string[] cards, int x)
        {
            var combinations = cards.ToCards().GetKCombinationsByRank(4);
            Assert.That(combinations.Count(), Is.EqualTo(x));
        }

        [TestCase(new string[] { "3Y", "5O", "7R", "9B" }, 1)]
        [TestCase(new string[] { "3Y", "3O", "7Y", "9B" }, 0)]
        [TestCase(new string[] { "3Y", "3O", "3B", "9B" }, 0)]
        [TestCase(new string[] { "3Y", "3R", "5O", "7B", "9G" }, 2)]
        [TestCase(new string[] { "3Y", "3G", "5O", "5Y", "7Y", "9B" }, 1)]
        [TestCase(new string[] { "3Y", "3G", "5Y", "5G", "7Y", "7G", "9Y", "9G" }, 2)]
        public void FindAllPossibleBombs_ShouldFind_XBombs(string[] cards, int x)
        {
            var combinations = cards.ToCards().FindAllPossibleBombs();
            Assert.That(combinations.Count(), Is.EqualTo(x));
        }

        [Test]
        public void FindTheSameCardsWithWildCards_ShouldFind_Triples_w_wilds()
        {
            var cards = new string[] { "3Y", "3O", "J" };
            var combinations = cards.ToCards().FindTheSameCardsWithWildCards(TrickType.TRIPLE);
            Assert.That(combinations.Count(), Is.EqualTo(1));
        }

        [Test]
        public void FindTheSameCardsWithWildCards_ShouldNotFind_Pair_w_wilds()
        {
            var cards = new string[] { "J", "Q" };
            var combinations = cards.ToCards().FindTheSameCardsWithWildCards(TrickType.PAIR);
            Assert.That(combinations.Count(), Is.EqualTo(0));

        }

        [Test]
        public void FindTheSameCardsWithWildCards_ShouldFind_Pair_when_more_wilds()
        {
            var cards = new string[] { "2O", "J", "Q" };
            var combinations = cards.ToCards().FindTheSameCardsWithWildCards(TrickType.PAIR);
            
            combinations.ForEach(x => Assert.That(x.Type , Is.EqualTo(TrickType.PAIR)));
            combinations.ForEach(x => Assert.That(x.Cards.Count(), Is.EqualTo(2)));

            Assert.That(combinations.Count(), Is.EqualTo(2));
        } 
        
        [Test]
        public void FindTheSameCardsWithWildCards_ShouldNotFind_SEQ3_when_only_wilds()
        {
            var cards = new string[] { "J", "Q", "K" };
            var combinations = cards.ToCards().FindTheSameCardsWithWildCards(TrickType.SEQ3);
            
            Assert.That(combinations.Count(), Is.EqualTo(0));
        }
        
        
        [TestCase(new string[] { "J", "Q", "K" }, 1)]
        public void FindAllPossibleBombs_ShouldFindBombs(string[] cards, int x)
        {
            var combinations = cards.ToCards().FindAllPossibleBombs();
            
            Assert.That(combinations.Count(), Is.EqualTo(4));
        }        
        
        [TestCase(new string[] { "2O", "2G"}, 1)]
        public void FindTheSameCardsWithWildCards_Should_notGetException_WhenNoWIlds(string[] cards, int x)
        {
            var combinations = cards.ToCards().FindTheSameCardsWithWildCards(TrickType.PAIR);
            
            Assert.That(combinations.Count(), Is.EqualTo(0));
        }

        [Test]
        public void FindPairedSequences_FindCardPairedSequences_ReturnsCorrectTrickTypes()
        {
          
            var cards = new List<Card>
                {
                    "3R".ToCard(), // THREE of RED
                    "3B".ToCard(), // THREE of BLACK
                    "3G".ToCard(), // THREE of GREEN
                    "4R".ToCard(), // FOUR of RED
                    "4B".ToCard(), // FOUR of BLACK
                    "5R".ToCard()  // FIVE of RED
                };

           
            var result = cards.FindPairedSequences(TrickType.PAIRSEQ2);

            var expectedTrick = new Trick(TrickType.PAIRSEQ2, new string[] { "3R", "3B", "4R", "4B" }.ToCards());
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(expectedTrick , Is.EqualTo(result[0])); //  dwie pary trójek i czwórek
            Assert.That(TrickType.PAIRSEQ2, Is.EqualTo(result[0].Type)); //  dwie pary trójek i czwórek


        }

        [Test]
        public void FindPairedSequences_FindCardPairedSequences_ReturnsCorrectTrickTypes_Multiple()
            {
                var cards = new string[] { "2R", "3R", "4R", "7R", "8R", "2B", "3B", "4B", "7B", "2G", "7G", "8G", "9G" }.ToCards();
                var result = cards.FindPairedSequences(TrickType.PAIRSEQ2);

                var expectedTrick = new Trick(TrickType.PAIRSEQ2, new string[] { "2R", "2B", "3R", "3B" }.ToCards());
                var expectedTrick1 = new Trick(TrickType.PAIRSEQ2, new string[] { "3R", "3B", "4R", "4B" }.ToCards());
                var expectedTrick2 = new Trick(TrickType.PAIRSEQ2, new string[] { "7R", "7G", "8R", "8G" }.ToCards());
                Assert.That(result.Count, Is.EqualTo(3));
                Assert.That(expectedTrick, Is.EqualTo(result[0])); 
                Assert.That(expectedTrick1, Is.EqualTo(result[1])); 
                Assert.That(expectedTrick2, Is.EqualTo(result[2])); 
                Assert.That(TrickType.PAIRSEQ2, Is.EqualTo(result[0].Type)); 
                Assert.That(TrickType.PAIRSEQ2, Is.EqualTo(result[1].Type)); 
                Assert.That(TrickType.PAIRSEQ2, Is.EqualTo(result[2].Type)); 
        }

        [Test]
        public void FindPairedSequences_FindCardPairedSequences_ReturnsEmptyList_WhenNoPairsExist()
        {
            var cards = new List<Card>
                {
                    "6R".ToCard(), // SIX of RED
                    "7B".ToCard()  // SEVEN of BLACK
                };

            // Act
            var result = cards.FindPairedSequences(TrickType.PAIRSEQ2);

            // Assert
            Assert.That(result, Is.Empty);
        }
    }
}