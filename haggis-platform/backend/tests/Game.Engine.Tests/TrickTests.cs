using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static Haggis.Domain.Enums.TrickType;

namespace HaggisTests
{
    [TestFixture]
    internal class TrickTests
    {

        [TestCase(PAIR, 1, 10)]
        [TestCase(PAIR, 2, 20)]
        [TestCase(PAIR, 3, 30)]
        [TestCase(TRIPLE, 1, 10)]
        [TestCase(TRIPLE, 2, 20)]
        [TestCase(TRIPLE, 3, 30)]
        [TestCase(QUAD, 1, 5)]
        [TestCase(QUAD, 2, 10)]
        [TestCase(QUAD, 3, 15)]
        [TestCase(FIVED, 1, 1)]
        [TestCase(null, 1, 31)]
        [TestCase(SEQ3, 1, 0)]
        [TestCase(SEQ3, 2, 0)]
        [TestCase(SEQ3, 3, 5)]
        [TestCase(SEQ3, 4, 10)]
        [TestCase(SEQ3, 5, 15)]
        [TestCase(SEQ4, 3, 0)]
        [TestCase(SEQ4, 4, 5)]
        [TestCase(SEQ4, 5, 10)]
        [TestCase(SEQ5, 3, 0)]
        [TestCase(SEQ5, 4, 0)]
        [TestCase(SEQ5, 5, 5)]
        public void FindTheSameCards_FindsTrick_ReturnsListWithTrick(TrickType? trickType, int numberOfCards, int expectedCount)
        {
            var player = new HaggisPlayer("AS");
            player.Hand = GenerateFiveTheSameCardsFrom2(numberOfCards);
            var possibleTricks = player.AllPossibleTricks(trickType);
            CheckTricks(possibleTricks);

            Assert.That(possibleTricks, Has.Exactly(expectedCount).Items);
        }

        [TestCase("4RY_PAIR", 4, 10)]
        [TestCase("4RY_PAIR", 3, 0)]
        [TestCase("4RYO_TRIPLE", 3, 0)]
        [TestCase("4RYO_TRIPLE", 4, 10)]
        [TestCase("2Y_SEQ3", 3, 0)]
        [TestCase("2Y_SEQ3", 4, 5)]
        [TestCase("3O_SEQ5", 3, 0)]
        [TestCase("4Y_SEQ5", 3, 0)]
        [TestCase("4Y_SEQ5", 5, 0)]
        [TestCase("2Y_SEQ5", 5, 0)]
        [TestCase("2Y_SEQ5", 6, 5)]
        public void FindsTrickGreaterThan(string trickString, int numberOfCards, int expectedCount)
        {
            var player = new HaggisPlayer("AS");
            player.Hand = GenerateFiveTheSameCardsFrom2(numberOfCards);

            var trick = trickString.ToTrick();
            var possibleTricks = player.SuggestedTricks(trick);

            CheckTricks(possibleTricks);

            Assert.That(possibleTricks, Has.Exactly(expectedCount).Items);
        }


        private List<Card> GenerateFiveTheSameCardsFrom2(int numberOfCards)
        {
            var cards = new List<Card>();
            for (int i = 0; i < numberOfCards; i++)
            {
                cards.Add(new Card((Rank)(i + 2), Suit.RED));
                cards.Add(new Card((Rank)(i + 2), Suit.BLACK));
                cards.Add(new Card((Rank)(i + 2), Suit.GREEN));
                cards.Add(new Card((Rank)(i + 2), Suit.YELLOW));
                cards.Add(new Card((Rank)(i + 2), Suit.ORANGE));
            }

            return cards;
        }

        private void CheckTricks(IEnumerable<Trick> tricks)
        {
            var sameCardTypes = new List<TrickType>() { SINGLE, PAIR, TRIPLE, QUAD, FIVED, SIXED };
            var sequenceTypes = new List<TrickType>() { SEQ3, SEQ4, SEQ5, SEQ6, SEQ6, SEQ7 };

            foreach (var trick in tricks)
            {
                if (sameCardTypes.Contains(trick.Type))
                {
                    var firstCard = trick.FirstCard();
                    foreach (var card in trick.Cards)
                    {
                        Assert.That(card.Rank, Is.EqualTo(firstCard.Rank));
                    }
                }
                if (sequenceTypes.Contains(trick.Type))
                {
                    var arrayTricks = trick.Cards.ToArray();
                    for (var i = 1; i < arrayTricks.Count() - 1; i++)
                    {
                        Assert.That(arrayTricks[i].RankDiff(arrayTricks[i - 1]), Is.EqualTo(1));
                    }
                }
            }
        }

        [TestCase(new string[] { "3Y", "5O", "7R", "9B" }, BombType.RAINBOW)]
        [TestCase(new string[] { "3Y", "5Y", "7Y", "9Y" }, BombType.COLOR)]

        public void CheckBombTypes(string[] cards, BombType bombType) {

            var combinations = cards.ToCards().FindAllPossibleBombs();

            Assert.That(combinations.First().Bomb, Is.EqualTo(bombType));
        }
        [Test]
        public void CheckNoBombTypes()
        {
            Assert.That("2YO_PAIR".ToTrick().Bomb, Is.Null);
            Assert.That("2Y_SEQ5".ToTrick().Bomb, Is.Null);
        }

        [Test]
        public void ShouldColorBombBeHigherThanRainbow() {

            var rainbow = new Trick(TrickType.BOMB, new string[] { "3Y", "5O", "7R", "9B" }.ToCards());
            var color = new Trick(TrickType.BOMB, new string[] { "3Y", "5Y", "7Y", "9Y" }.ToCards());

            Assert.That(rainbow.CompareTo(color), Is.LessThan(0));
            Assert.That(color.CompareTo(rainbow), Is.GreaterThan(0));
        }
        [Test]
        public void ShouldBombBeHigherThanTrick() {

            var rainbow = new Trick(TrickType.BOMB, new string[] { "3Y", "5O", "7R", "9B" }.ToCards());
            var color = new Trick(TrickType.BOMB, new string[] { "3Y", "5Y", "7Y", "9Y" }.ToCards());

            Assert.That(rainbow.CompareTo("2O_SINGLE".ToTrick()), Is.GreaterThan(0));
            Assert.That(color.CompareTo("2O_SINGLE".ToTrick()), Is.GreaterThan(0));
        }

        [Test]
        public void ShouldBombBeHigherThanOtherBombs()
        {

            var JQ = new string[] { "J", "Q" }.ToCards();
            var JK = new string[] { "J", "K" }.ToCards();
            var QK = new string[] { "Q", "K" }.ToCards();
            var JQK = new string[] { "J", "Q", "K" }.ToCards();


            var trick1 = new Trick(TrickType.BOMB, JQ);
            var trick2 = new Trick(TrickType.BOMB, JK);
            var trick3 = new Trick(TrickType.BOMB, QK);
            var trick4 = new Trick(TrickType.BOMB, JQK);


            Assert.That(trick1.CompareTo(trick2), Is.EqualTo(-1));
            Assert.That(trick2.CompareTo(trick3), Is.EqualTo(-1));
            Assert.That(trick3.CompareTo(trick4), Is.EqualTo(-1));
            Assert.That(trick4.CompareTo(trick1), Is.EqualTo(1));


        }

        [Test]
        public void ShouldBeEqualWhenWildTrick()
        {
            var wildCardAs = new Card(Rank.JACK).WildAs(new Card(Rank.FIVE, Suit.ORANGE));
            var cards = new string[] { "3Y" }.ToCards();
            cards.Add(wildCardAs);
            var wildedTrick = new Trick(TrickType.PAIR, cards);
            var pair = new Trick(TrickType.PAIR, new string[] { "3Y", "3R"}.ToCards());
            Assert.That(pair.CompareTo(wildedTrick), Is.EqualTo(0));

        }

        [Test]
        public void ShouldBeHigherWhenWildPairTrick()
        {
            var wildCardAs = new Card(Rank.JACK).WildAs(new Card(Rank.FIVE, Suit.ORANGE));
            var cards = new string[] { "5Y" }.ToCards();
            cards.Add(wildCardAs);
            var wildedTrick = new Trick(TrickType.PAIR, cards);
            var pair = new Trick(TrickType.PAIR, new string[] { "3Y", "3R"}.ToCards());
            Assert.That(pair.CompareTo(wildedTrick), Is.LessThan(0));

        } 
        
        [TestCase(new string[] { "3Y", "4Y", "5Y" }, 0)]
        public void ShouldBeHigherWhenWildSequenceTrick(string[] cards, int expectedCompare)
        {
            var wildCardAs = new Card(Rank.JACK).WildAs(new Card(Rank.SIX, Suit.ORANGE));
            var wildedCards = new string[] { "4O", "5O" }.ToCards();
            wildedCards.Add(wildCardAs);
            
            var wildedTrick = new Trick(TrickType.SEQ3, wildedCards);
            var trick = new Trick(TrickType.SEQ3, cards.ToCards());
            Assert.That(wildedTrick.CompareTo(trick), Is.GreaterThan(expectedCompare));

        }
        
        [TestCase(new string[] { "3Y", "4Y", "5Y" }, 0)]
        public void DoesNotMatterWhereWildIs(string[] cards, int expectedCompare)
        {
            var wildCardAs = new Card(Rank.JACK).WildAs(new Card(Rank.SIX, Suit.ORANGE));
            var wildedCards = new string[] { "4O", "5O" }.ToCards();
            wildedCards.Add(wildCardAs);

            var wildedTrick = new Trick(TrickType.SEQ3, wildedCards);
            var trick = new Trick(TrickType.SEQ3, cards.ToCards());
            Assert.That(wildedTrick.CompareTo(trick), Is.GreaterThan(expectedCompare));


            wildCardAs = new Card(Rank.JACK).WildAs(new Card(Rank.FIVE, Suit.ORANGE));
            wildedCards = new string[] { "4O", "6O" }.ToCards();
            wildedCards.Add(wildCardAs);
            
            wildedTrick = new Trick(TrickType.SEQ3, wildedCards);
            trick = new Trick(TrickType.SEQ3, cards.ToCards());
            Assert.That(wildedTrick.CompareTo(trick), Is.GreaterThan(expectedCompare));
            
            wildCardAs = new Card(Rank.JACK).WildAs(new Card(Rank.FOUR, Suit.ORANGE));
            wildedCards = new string[] { "5O", "6O" }.ToCards();
            wildedCards.Add(wildCardAs);
            
            wildedTrick = new Trick(TrickType.SEQ3, wildedCards);
            trick = new Trick(TrickType.SEQ3, cards.ToCards());
            Assert.That(wildedTrick.CompareTo(trick), Is.GreaterThan(expectedCompare));

        }

        [TestCase("2R_SINGLE", "2R_SINGLE", 0)]
        [TestCase("2R_SINGLE", "2G_SINGLE", 0)]
        [TestCase("2R_SINGLE", "2RB_PAIR", -1)]
        [TestCase("2RGB_TRIPLE", "2RB_PAIR", 1)]
        [TestCase("2RGBO_QUAD", "2B_SEQ3", 1)]
        [TestCase("2RGBO_QUAD", "2B_SEQ4", -1)]
        [TestCase("5RGBO_QUAD", "2B_SEQ4", -1)]
        [TestCase("5RGBO_QUAD", "6RGBO_QUAD", -1)]

        public void ShouldCompareTricks(string trick1, string trick2, int result) { 
        
            Assert.That(trick1.ToTrick().CompareTo(trick2.ToTrick()), Is.EqualTo(result));

        }
    }
}
