using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using NUnit.Framework;
using System.Collections.Generic;

namespace HaggisTests
{
    [TestFixture]
    internal class ScoringStrategiesTests
    {
        [Test]
        public void ClassicScoringStrategy_ShouldScoreConfiguredRanks()
        {
            var strategy = new ClassicHaggisScoringStrategy();

            Assert.That(strategy.GetCardPoints(new Card(Rank.THREE, Suit.RED)), Is.EqualTo(1));
            Assert.That(strategy.GetCardPoints(new Card(Rank.FIVE, Suit.GREEN)), Is.EqualTo(1));
            Assert.That(strategy.GetCardPoints(new Card(Rank.SEVEN, Suit.ORANGE)), Is.EqualTo(1));
            Assert.That(strategy.GetCardPoints(new Card(Rank.NINE, Suit.BLACK)), Is.EqualTo(1));
            Assert.That(strategy.GetCardPoints(new Card(Rank.JACK)), Is.EqualTo(2));
            Assert.That(strategy.GetCardPoints(new Card(Rank.QUEEN)), Is.EqualTo(3));
            Assert.That(strategy.GetCardPoints(new Card(Rank.KING)), Is.EqualTo(5));
        }

        [Test]
        public void ClassicScoringStrategy_ShouldReturnZeroForUnscoredCardsAndNull()
        {
            var strategy = new ClassicHaggisScoringStrategy();

            Assert.That(strategy.GetCardPoints(new Card(Rank.TWO, Suit.YELLOW)), Is.EqualTo(0));
            Assert.That(strategy.GetCardPoints(new Card(Rank.TEN, Suit.YELLOW)), Is.EqualTo(0));
            Assert.That(strategy.GetCardPoints(null), Is.EqualTo(0));
        }

        [Test]
        public void EveryCardOnePointStrategy_ShouldScoreNonWildAsOne()
        {
            var strategy = new EveryCardOnePointScoringStrategy();

            Assert.That(strategy.GetCardPoints(new Card(Rank.TWO, Suit.BLACK)), Is.EqualTo(1));
            Assert.That(strategy.GetCardPoints(new Card(Rank.SIX, Suit.GREEN)), Is.EqualTo(1));
            Assert.That(strategy.GetCardPoints(new Card(Rank.TEN, Suit.ORANGE)), Is.EqualTo(1));
        }

        [Test]
        public void EveryCardOnePointStrategy_ShouldKeepWildBonuses()
        {
            var strategy = new EveryCardOnePointScoringStrategy();

            Assert.That(strategy.GetCardPoints(new Card(Rank.JACK)), Is.EqualTo(2));
            Assert.That(strategy.GetCardPoints(new Card(Rank.QUEEN)), Is.EqualTo(3));
            Assert.That(strategy.GetCardPoints(new Card(Rank.KING)), Is.EqualTo(5));
        }

        [Test]
        public void EveryCardOnePointStrategy_ShouldKeepWildBonusWhenWildReplacesCard()
        {
            var strategy = new EveryCardOnePointScoringStrategy();
            var wildJackAsTwo = new Card(Rank.JACK).WildAs(new Card(Rank.TWO, Suit.RED));

            Assert.That(strategy.GetCardPoints(wildJackAsTwo), Is.EqualTo(2));
        }

        [Test]
        public void Strategies_ShouldExposeConfiguredRunOutMultiplier()
        {
            var classic = new ClassicHaggisScoringStrategy(runOutMultiplier: 7);
            var everyCard = new EveryCardOnePointScoringStrategy(runOutMultiplier: 9);

            Assert.That(classic.RunOutMultiplier, Is.EqualTo(7));
            Assert.That(everyCard.RunOutMultiplier, Is.EqualTo(9));
        }

        [Test]
        public void Strategies_ShouldExposeConfiguredGameOverScore()
        {
            var classic = new ClassicHaggisScoringStrategy(gameOverScore: 300);
            var everyCard = new EveryCardOnePointScoringStrategy(gameOverScore: 200);
            var configurable = new ConfigurableHaggisScoringStrategy(
                cardPointsByRank: new Dictionary<Rank, int>(),
                runOutMultiplier: 5,
                gameOverScore: 180);

            Assert.That(classic.GameOverScore, Is.EqualTo(300));
            Assert.That(everyCard.GameOverScore, Is.EqualTo(200));
            Assert.That(configurable.GameOverScore, Is.EqualTo(180));
        }
    }
}
