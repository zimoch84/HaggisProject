using System;
using System.Linq;
using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests
{
    [TestFixture]
    public class HaggisDeckDealerTests
    {
        [Test]
        public void DealSetupCards_ShouldConsumeCardsFromDealerState()
        {
            var dealer = new HaggisDeckDealer(12345);

            var dealt = dealer.DealSetupCards();
            var remaining = dealer.GetHaggisCards();

            Assert.That(dealt.Count, Is.EqualTo(17));
            Assert.That(dealt.Count(card => card.BaseRank == Rank.JACK || card.BaseRank == Rank.QUEEN || card.BaseRank == Rank.KING), Is.EqualTo(3));
            Assert.That(remaining.Count, Is.EqualTo(31));
        }

        [Test]
        public void GetHaggisCards_ShouldReturnRemainingUndealtCards()
        {
            var dealer = new HaggisDeckDealer(12345);

            dealer.DealSetupCards();
            dealer.DealSetupCards();
            dealer.DealSetupCards();

            var remaining = dealer.GetHaggisCards();

            Assert.That(remaining.Count, Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() => dealer.DealSetupCards());
        }
    }
}
