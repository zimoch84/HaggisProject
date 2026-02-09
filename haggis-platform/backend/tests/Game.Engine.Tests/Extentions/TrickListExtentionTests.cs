using Haggis.Enums;
using Haggis.Extentions;
using Haggis.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaggisTests
{
    [TestFixture]
    internal class TrickListExtentionTests
    {

        List<Trick> tricks;

        [SetUp]
        public void SetUp()
        {
            tricks = new List<Trick>();
            tricks.Add("2G_SINGLE".ToTrick());
            tricks.Add("6GO_PAIR".ToTrick());
            tricks.Add("7GO_PAIR".ToTrick());
            tricks.Add("4G_SINGLE".ToTrick());
            tricks.Add("4BYO_TRIPLE".ToTrick());
            tricks.Add("5BYOG_QUAD".ToTrick());
            tricks.Add("6BYOG_QUAD".ToTrick());
            tricks.Add("5G_SINGLE".ToTrick());
            tricks.Add("8G_SINGLE".ToTrick());
            tricks.Add("8BYOG_QUAD".ToTrick());
            tricks.Add("2O_SEQ3".ToTrick());
            tricks.Add("3O_SEQ3".ToTrick());

            var wildCards = new string[] { "5O", "6O"}.ToCards();
            wildCards.Add(new Card(Rank.JACK).WildAs("7O".ToCard()));

            tricks.Add(new Trick(TrickType.SEQ3, wildCards));

        }


        [TestCase("2G_SINGLE", true)]
        [TestCase("5GO_PAIR", true)]
        [TestCase("2O_SEQ3", false)]
        [TestCase("2B_SEQ3", true)]
        public void ShouldHaveContinuation(string comparedTrick, bool shouldHaveContinuation) {

            var hasContinuation = tricks.HasContinuation(comparedTrick.ToTrick());

            Assert.That(shouldHaveContinuation, Is.EqualTo(hasContinuation));

        }

        [TestCase("2G_SINGLE", true)]
        [TestCase("5GO_PAIR", true)]
        [TestCase("2O_SEQ3", true)]
        [TestCase("2B_SEQ3", true)]
        public void ShouldHaveContinuationWithWild(string comparedTrick, bool shouldHaveContinuation)
        {
            var hasContinuation = tricks.HasContinuationWithWilds(comparedTrick.ToTrick());

            Assert.That(shouldHaveContinuation, Is.EqualTo(hasContinuation));

        }
    }
}
